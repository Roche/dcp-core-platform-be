using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Datasource.Base.Interfaces;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class DatasourceService : BaseService, IDatasourceService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        protected ISecretConfigurationProvider _configProvider;
        protected IServiceProvider _serviceProvider;

        protected DatasourceCache _cache;
        private readonly DbConnectionStringLoader _loader;

        public DatasourceService(DCPCoreDbContext dbContext, IConfiguration configuration,
            ILoggerFactory factory,
            IMemoryCache memoryCache,
            IServiceDiscovery serviceDiscovery,
            IServiceProvider serviceProvider,
            ISecretConfigurationProvider configProvider,
            DatasourceCache cache
            ) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _serviceProvider = serviceProvider;
            _configProvider = configProvider;
            _cache = cache;

            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }
        public async Task<DatasourceConfigurationForm> CreateDatasourceAsync(DatasourceUserConfigurationDTO model, UserContextDTO user)
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

            if (_dbContext.DataSource.Any(x => x.Name == model.Name && x.SiteId == model.SiteId))
            {
                throw new DCPBadRequestException(ErrorMessages.DatasourceAlreadyExist);
            }

            //Connection string validation
            var key = GetEncryptionKey();

            var connStringPipeline = new PluginPipeline<object[], string>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var encrypted = await connStringPipeline.RunOnConnectionTypeAsync<IDatasource>(
                async (ds, args, ct) =>
                {
                    string encrypted = string.Empty;

                    try
                    {
                        var json = ds.TransformConnectionString((List<FormValue>)args[0]);
                        var fixedJson = UnescapeSpecialChars(json);
                        await ds.ValidateConnectionString(fixedJson);
                        encrypted = ds.GetEncryptedConnectionStringAsync(json, key);
                    }
                    catch (Exception ex)
                    {
                        throw new DCPBadRequestException($"{model.Name} - {ex.Message}");
                    }

                    return encrypted;
                },
                connectionType: model.Type,
                args: new object[] { model.ConnectionString }
            );

            var datasource2Add = new DataSource
            {
                Name = model.Name,
                Type = model.Type,
                SiteId = model.SiteId.HasValue ? model.SiteId.Value : 0,
                Reason = model.ChangeReason,
                IsValidated = model.IsValidated,
                ConnectionString = encrypted
            };

            await _dbContext.AddAsync(datasource2Add);
            await _dbContext.SaveChangesAsync();

            await SaveDatasourceSecretAsync(datasource2Add);

            return await BuildConfigurationFormAsync(datasource2Add.Id);
        }

        public async Task DeleteDatasourceAsync(int datasourceId, string reason, UserContextDTO user)
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

            var dataSource = await _dbContext.DataSource.FindAsync(datasourceId);

            if (dataSource == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownDatasource);
            }

            //Get related records, with audit trail tracking
            var datatypes = await _dbContext.DataType
                .Where(x => x.DataSource!.SiteId == dataSource.SiteId)
                .ToListAsync();

            //Perform the update to maintain the audit trail
            dataSource.LastModifiedBy = user.UserId;
            dataSource.Reason = reason;

            // Validate that the data type to be deleted is not a mandatory data type for any module
            var key = GetEncryptionKey();
            var env = _configuration["Host:EnvironmentSlug"];

            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend" }, env);

            var dataTypeSettingsPerModule = services
                .Where(s => !string.IsNullOrWhiteSpace(s.MetaData?.ModuleName))
                .ToDictionary(s => s.MetaData.ModuleName, s => s.MetaData.DataTypes);

            var dataTypeRequirements = datatypes
                .GroupBy(x => new { x.ModuleName, x.Type })
                .Select(x => new
                {
                    ConfiguredDatasources = x.Select(y => y.DataSourceId).ToList(),
                    ModuleName = x.Key.ModuleName,
                    Type = x.Key.Type,
                    IsMandatory = dataTypeSettingsPerModule.ContainsKey(x.Key.ModuleName) && dataTypeSettingsPerModule[x.Key.ModuleName].First(dt => dt.Type == x.Key.Type).Mandatory
                });

            if (dataTypeRequirements.Any(x => x.ConfiguredDatasources.Except(new List<int> { datasourceId }).Count() == 0 && x.IsMandatory))
            {
                throw new DCPBadRequestException(ErrorMessages.MissingRequiredDataType);
            }

            //Validate that no connection bridge is using the datasource to be deleted
            if (_dbContext.ConnectionBridge.Any(x => x.SConId == datasourceId || x.TConId == datasourceId))
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidConnectionBridges);
            }

            datatypes = datatypes.Where(x => x.DataSourceId == datasourceId).ToList();

            //Perform the update to maintain the audit trail
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
            _dbContext.DataSource.Remove(dataSource);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<DatasourceConfigurationForm> GetSingleDatasourceAsync(int datasourceId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            return await BuildConfigurationFormAsync(datasourceId);
        }

        public async Task<List<DatasourceListItemDTO>> SearchDatasourceDetailsAsync(DatasourceQueryDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var result = await _dbContext.DataSource
               .Where(x => model.SiteId.Count == 0 || model.SiteId.Contains(x.SiteId))
               .Select(x => new DatasourceListItemDTO
               {
                   Id = x.Id,
                   Name = x.Name,
                   ConnectionType = x.Type
               })
               .AsNoTracking()
               .ToListAsync();

            return result;
        }

        public async Task<DatasourceConfigurationForm> UpdateDatasourceAsync(int siteId, DatasourceUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (user.UserId is null)
                throw new DCPForbiddenException(ErrorMessages.UnkownUser);

            if (string.IsNullOrEmpty(model.ChangeReason))
            {
                throw new DCPBadRequestException(ErrorMessages.ChangeReasonRequired);
            }

            var dataSource = await _dbContext.DataSource.FindAsync(model.Id);

            if (dataSource == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownDatasource);
            }

            //Connection string validation
            var key = GetEncryptionKey();

            var connStringPipeline = new PluginPipeline<object[], string>(_cache, _serviceProvider, _loader)
                .WithConcretMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();


            var encrypted = await connStringPipeline.RunMapReduceAsync<IDatasource>(
                async (ds, args, ct) =>
                {
                    string encrypted = string.Empty;

                    try
                    {
                        var json = ds.TransformConnectionString((List<FormValue>)args[0]);
                        var fixedJson = UnescapeSpecialChars(json);
                        await ds.ValidateConnectionString(fixedJson);
                        encrypted = ds.GetEncryptedConnectionStringAsync(json, key);
                    }
                    catch (Exception ex)
                    {
                        throw new DCPBadRequestException($"{model.Name} - {ex.Message}");
                    }

                    return encrypted;
                },
                args: new object[] { model.Id, model.ConnectionString },
                mapArgIndex: 0
            );

            dataSource.Name = model.Name;
            dataSource.Type = model.Type;
            dataSource.ConnectionString = encrypted;
            dataSource.IsValidated = model.IsValidated;

            if (_dbContext.Entry(dataSource).State == EntityState.Modified)
            {
                dataSource.LastModifiedBy = user.UserId;
                dataSource.Reason = model.ChangeReason;
            }


            await _dbContext.SaveChangesAsync();

            await SaveDatasourceSecretAsync(dataSource);

            return await BuildConfigurationFormAsync(dataSource.Id);
        }

        private async Task<DatasourceConfigurationForm> BuildConfigurationFormAsync(int datasourceId)
        {
            var datasource = await _dbContext.DataSource.FindAsync(datasourceId);

            if (datasource == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownDatasource);
            }

            var form = new DatasourceConfigurationForm
            {
                Id = datasource.Id,
                IsValidated = datasource.IsValidated,
                Type = datasource.Type,
                Name = datasource.Name,
            };

            return form;
        }

        private static JsonObject UnescapeSpecialChars(JsonObject json)
        {
            var jsonString = JsonSerializer.Serialize(json);
            jsonString = jsonString
                .Replace("\\\\n", "\\n")
                .Replace("\\\\t", "\\t")
                .Replace("\\\\r", "\\r");
            return JsonSerializer.Deserialize<JsonObject>(jsonString)!;
        }

        private async Task SaveDatasourceSecretAsync(DataSource dataSource)
        {
            var key = GetEncryptionKey();
            var relativePath = $"{_configuration["Host:EnvironmentSlug"]}/{dataSource.Id}";

            var decryptedJson = EncryptionHelper.DecryptConnectionString(dataSource.ConnectionString, key);

            var secret = JsonSerializer.Deserialize<Dictionary<string, object>>(decryptedJson);

            await _configProvider.SaveSecretAsync(relativePath, secret);

        }
    }
}
