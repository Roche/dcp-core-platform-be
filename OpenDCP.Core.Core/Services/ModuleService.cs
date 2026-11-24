using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class ModuleService : BaseService, IModuleService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        protected ISecretConfigurationProvider _configProvider;
        protected IServiceProvider _serviceProvider;


        public ModuleService(DCPCoreDbContext dbContext, IConfiguration configuration,
            ILoggerFactory factory,
            IMemoryCache memoryCache,
            IServiceDiscovery serviceDiscovery,
            IServiceProvider serviceProvider,
            ISecretConfigurationProvider configProvider
            ) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _serviceProvider = serviceProvider;
            _configProvider = configProvider;
        }

        public async Task<ModuleConfigurationForm> CreateModuleAsync(ModuleUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (user.UserId is null)
                throw new DCPForbiddenException(ErrorMessages.UnkownUser);

            if (string.IsNullOrEmpty(model.ChangeReason))
            {
                throw new DCPBadRequestException(ErrorMessages.ChangeReasonRequired);
            }

            if (_dbContext.Module.Any(x => x.Name == model.Name && x.SiteId == model.SiteId))
            {
                throw new DCPBadRequestException(ErrorMessages.ModuleAlreadyExist);
            }

            var env = _configuration["Host:EnvironmentSlug"];

            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend" }, env);

            var moduleNames = services
                .Select(s => s.MetaData?.ModuleName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!moduleNames.Contains(model.Name))
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            //Connection string validation
            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);
            var key = GetEncryptionKey();

            var moduleInstance = calculationNodeFactory.LoadByType(model.Type);

            try
            {
                moduleInstance.UpdateConnectionString(model.ConnectionString);
                await moduleInstance.ValidateConnectionString();
            }
            catch (Exception ex)
            {
                throw new DCPBadRequestException($"{model.Name} - {ex.Message}");
            }

            //DataType validation
            //ToDo - skipped for inactive modules
            //Add validation if datasource implements the send data type interface, if not throw error
            var moduleDataTypes2add = new List<DataType>();
            var moduleDataTypeSettings = services
                    .Where(x => string.Equals(
                        x.MetaData.ModuleName,
                        model.Name,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.MetaData.DataTypes)
                    .First();

            foreach (var dataTypeSetting in moduleDataTypeSettings)
            {
                if (dataTypeSetting.Mandatory
                    && !model.DataTypes.Any(dt => dt.DataType == dataTypeSetting.Type))
                {
                    throw new DCPBadRequestException($"{dataTypeSetting.Type} - " + ErrorMessages.MissingRequiredDataType);
                }

                if (!dataTypeSetting.AllowMultiselection && model.DataTypes.Count(x => x.DataType == dataTypeSetting.Type) > 1)
                {
                    throw new DCPBadRequestException($"{dataTypeSetting.Type} - " + ErrorMessages.InvalidMultiDataTypeMapping);
                }
            }

            var moduleDataTypes = model.DataTypes
                .Where(x => x.Id == null)
                .Select(x => new DataType
                {
                    DataSourceId = x.DatasourceId,
                    LastModifiedBy = user.UserId,
                    OwnerUserId = user.UserId,
                    Reason = string.Empty,
                    ModuleName = model.Name,
                    Type = x.DataType
                })
                .ToList();

            _dbContext.DataType.AddRange(moduleDataTypes);

            var module2add = new Module
            {
                ConnectionString = moduleInstance.GetEncryptedConnectionStringAsync(key),
                IsValidated = model.IsValidated,
                LastModifiedBy = user.UserId,
                IsEnable = model.IsEnabled,
                Name = model.Name,
                Reason = model.ChangeReason,
                SiteId = model.SiteId ?? 0,
                OwnerUserId = user.UserId,
                Type = model.Type
            };

            _dbContext.Module.Add(module2add);
            await _dbContext.SaveChangesAsync();

            return await BuildConfigurationFormAsync(module2add.Id);
        }

        public async Task DeleteModuleAsync(int moduleId, string reason, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (user.UserId is null)
                throw new DCPForbiddenException(ErrorMessages.UnkownUser);

            if (string.IsNullOrEmpty(reason))
            {
                throw new DCPBadRequestException(ErrorMessages.ChangeReasonRequired);
            }

            var module = await _dbContext.Module.FindAsync(moduleId);

            if (module == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            //Get related records, with audit trail tracking
            var datatypes = await _dbContext.DataType
                .Where(x => x.DataSource!.SiteId == module.SiteId && x.ModuleName == module.Name)
                .ToListAsync();

            //Perform the update to maintain the audit trail
            module.LastModifiedBy = user.UserId;
            module.Reason = reason;

            datatypes.ForEach(x =>
            {
                x.LastModifiedBy = user.UserId;
                x.Reason = reason;
            });

            await _dbContext.SaveChangesAsync();

            //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            //Remove the records
            _dbContext.DataType.RemoveRange(datatypes);
            _dbContext.Module.Remove(module);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<ModuleConfigurationForm> GetSingleModuleAsync(int moduleId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            return await BuildConfigurationFormAsync(moduleId);
        }

        public async Task<List<ModuleListItemDTO>> SearchModuleDetailsAsync(ModuleQueryDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var result = await _dbContext.Module
               .Where(x => model.SiteId.Count == 0 || model.SiteId.Contains(x.SiteId))
               .Select(x => new ModuleListItemDTO
               {
                   Id = x.Id,
                   Name = x.Name,
                   IsEnabled = x.IsEnable,
               })
               .AsNoTracking()
               .ToListAsync();

            return result;
        }

        public async Task<ModuleConfigurationForm> UpdateModuleAsync(int moduleId, ModuleUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (user.UserId is null)
                throw new DCPForbiddenException(ErrorMessages.UnkownUser);

            if (string.IsNullOrEmpty(model.ChangeReason))
            {
                throw new DCPBadRequestException(ErrorMessages.ChangeReasonRequired);
            }

            var module = _dbContext.Module.Find(moduleId);

            if (module is null)
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);


            var env = _configuration["Host:EnvironmentSlug"];
            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend" }, env);

            var moduleNames = services
                .Select(s => s.MetaData?.ModuleName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!moduleNames.Contains(model.Name))
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            //Connection string validation
            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);
            var key = GetEncryptionKey();

            var moduleInstance = await calculationNodeFactory.LoadByIdAsync(moduleId, model.Type);

            try
            {
                moduleInstance.UpdateConnectionString(model.ConnectionString);
                await moduleInstance.ValidateConnectionString();
            }
            catch (Exception ex)
            {
                throw new DCPBadRequestException($"{model.Name} - {ex.Message}");
            }

            //DataType validation
            //ToDo - skipped for inactive modules
            var moduleDataTypeSettings = services
                    .Where(x => string.Equals(
                        x.MetaData.ModuleName,
                        model.Name,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.MetaData.DataTypes)
                    .First();

            var existingDataTypes = await _dbContext.DataType
               .Where(x => x.ModuleName == model.Name && x.DataSource!.SiteId == module.SiteId)
               .ToListAsync();

            foreach (var dataTypeSetting in moduleDataTypeSettings)
            {
                if (dataTypeSetting.Mandatory
                    && !model.DataTypes.Any(dt => dt.DataType == dataTypeSetting.Type))
                {
                    throw new DCPBadRequestException($"{dataTypeSetting.Type} - " + ErrorMessages.MissingRequiredDataType);
                }

                if (!dataTypeSetting.AllowMultiselection && model.DataTypes.Count(x => x.DataType == dataTypeSetting.Type) > 1)
                {
                    throw new DCPBadRequestException($"{dataTypeSetting.Type} - " + ErrorMessages.InvalidMultiDataTypeMapping);
                }
            }

            //Newly added data types to add
            var moduleDataTypes2add = model.DataTypes
                .Where(x => x.Id == null)
                .Select(x => new DataType
                {
                    DataSourceId = x.DatasourceId,
                    LastModifiedBy = user.UserId,
                    OwnerUserId = user.UserId,
                    Reason = string.Empty, //ToDo add changeReason,
                    ModuleName = model.Name,
                    Type = x.DataType
                })
                .ToList();

            //Updated data types to update
            foreach (var dataType in model.DataTypes.Where(x => x.Id != null))
            {
                var existingDataType = existingDataTypes.FirstOrDefault(x => x.Id == dataType.Id!.Value);
                if (existingDataType != null)
                {
                    existingDataType.DataSourceId = dataType.DatasourceId;
                    existingDataType.Type = dataType.DataType;

                    if (_dbContext.Entry(existingDataType).State == EntityState.Modified)
                    {
                        existingDataType.LastModifiedBy = user.UserId;
                        existingDataType.Reason = model.ChangeReason;
                    }
                }
            }

            //Removed data types to delete
            var moduleDataTypes2remove = existingDataTypes
                .Where(x => !model.DataTypes.Any(dt => dt.Id == x.Id))
                .ToList();

            if (moduleDataTypes2remove.Count > 0)
            {
                //Perform the update to maintain the audit trail
                moduleDataTypes2remove.ForEach(x =>
                {
                    x.LastModifiedBy = user.UserId;
                    x.Reason = model.ChangeReason;
                });
                await _dbContext.SaveChangesAsync();
                //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
                await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);
                _dbContext.DataType.RemoveRange(moduleDataTypes2remove);
            }

            _dbContext.DataType.AddRange(moduleDataTypes2add);

            module.ConnectionString = moduleInstance.GetEncryptedConnectionStringAsync(key);
            module.Type = model.Type;
            module.Name = model.Name;
            module.IsValidated = model.IsValidated;
            module.IsEnable = model.IsEnabled;

            if (_dbContext.Entry(module).State == EntityState.Modified)
            {
                module.LastModifiedBy = user.UserId;
                module.Reason = model.ChangeReason;
            }

            await _dbContext.SaveChangesAsync();

            return await BuildConfigurationFormAsync(module.Id);
        }

        private async Task<ModuleConfigurationForm> BuildConfigurationFormAsync(int moduleId)
        {
            var module = await _dbContext.Module.FindAsync(moduleId);

            if (module == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            var form = new ModuleConfigurationForm
            {
                Id = module.Id,
                IsEnabled = module.IsEnable,
                IsValidated = module.IsValidated,
                Type = module.Type,
                Name = module.Name,
                DataTypes = await _dbContext.DataType
                    .Where(dt => dt.ModuleName == module.Name && dt.DataSource!.SiteId == module.SiteId)
                    .Select(dt => new DataTypeConfigurationFormDTO
                    {
                        Id = dt.Id,
                        DatasourceId = dt.DataSourceId,
                        DataType = dt.Type
                    })
                    .ToListAsync()
            };
            return form;
        }
    }
}
