using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.DAL.Extensions;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Legacy.Dashboard;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.AuditTrail.Classes;
using OpenDCP.Framework.AuditTrail.Models;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.Connections.Manager.Constants;
using OpenDCP.Framework.Connections.Manager.Factories;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Dashboard.Models;
using OpenDCP.Framework.Datasource.Base.Interfaces;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.Messaging.Message.EventBus;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Module = OpenDCP.Core.DAL.Entities.Module;

namespace OpenDCP.Core.Core.Services
{
    public class SiteService : BaseService, ISiteService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IMessageBus _messageBus;
        protected ISecretConfigurationProvider _configProvider;
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;
        private readonly DbConnectionStringLoader _loader;
        public SiteService(DCPCoreDbContext dbContext, IConfiguration configuration,
            ILoggerFactory factory,
            IMemoryCache memoryCache,
            IServiceDiscovery serviceDiscovery,
            IServiceProvider serviceProvider,
            ISecretConfigurationProvider configProvider,
            DatasourceCache cache,
            IMessageBus messageBus) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _serviceProvider = serviceProvider;
            _configProvider = configProvider;
            _cache = cache;
            _messageBus = messageBus;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<SiteConfigurationFormDTO> CreateSiteAsync(SiteUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (user.UserId is null)
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);

            if (await _dbContext.Site.AnyAsync(x => x.Name == model.Name))
            {
                throw new DCPBadRequestException(ErrorMessages.SiteNameExist);
            }

            var site = new Site
            {
                IsEnable = model.IsEnabled,
                LastModifiedBy = user.UserId,
                Name = model.Name,
                OwnerUserId = user.UserId,
                ShortName = model.ShortName,
                SiteType = model.Type,
                TimeZone = model.TimeZone,
                Reason = model.ChangeReason
            };

            await _dbContext.Site.AddAsync(site);

            var env = _configuration["Host:EnvironmentSlug"];
            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend" }, env);

            //Module handling
            await AddNewModulesAsync(site, model.Modules, user.UserId.Value, model.ChangeReason, services);

            //Datasource handling
            await AddNewDatasourcesAsync(site, model.Datasources, user.UserId.Value, model.ChangeReason);

            return await BuildSiteConfigurationFormAsync(site.Id);
        }
        public async Task DeleteSiteAsync(int siteId, string reason, UserContextDTO user)
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

            var site = await _dbContext.Site.FindAsync(siteId);

            if (site == null || siteId == 0)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            var users = await _dbContext.User
                .Where(u => u.Permissions.Any(p => p.SiteId == siteId))
                .ToListAsync();

            var unixNamesOnSite = users
                .Where(x => x.CurrentSiteId == siteId)
                .Select(x => x.UnixId)
                .ToList();

            if (await _dbContext.PersitedGrant.AnyAsync(p => unixNamesOnSite.Contains(p.UnixId)))
            {
                throw new DCPForbiddenException(ErrorMessages.DeleteSiteWithUsers);
            }


            //Get related records, with audit trail tracking
            var datasources = await _dbContext.DataSource
                .Where(x => x.SiteId == siteId)
                .ToListAsync();

            var datatypes = await _dbContext.DataType
                .Where(x => x.DataSource!.SiteId == siteId)
                .ToListAsync();

            var modules = await _dbContext.Module
                .Where(x => x.SiteId == siteId)
                .ToListAsync();

            var connectionBridges = await _dbContext.ConnectionBridge
                .Where(x => x.SCon.SiteId == siteId)
                .ToListAsync();

            //Get related records, without audit trail tracking
            var activeEvents = await _dbContext.InProgressEvent
                .Where(x => x.Datasource!.SiteId == siteId)
                .ToListAsync();

            var systemShareLinks = await _dbContext.ShareLinkSystemGenerated
                .Where(x => x.SiteId == siteId)
                .ToListAsync();

            //Perform the update to maintain the audit trail
            site.LastModifiedBy = user.UserId;
            site.Reason = reason;

            users.ForEach(x =>
            {
                x.Permissions = x.Permissions
                    .Where(p => p.SiteId != siteId)
                    .ToList();
            });

            datasources.ForEach(x =>
            {
                x.LastModifiedBy = user.UserId;
                x.Reason = reason;
            });

            modules.ForEach(x =>
            {
                x.LastModifiedBy = user.UserId;
                x.Reason = reason;
            });

            datatypes.ForEach(x =>
            {
                x.LastModifiedBy = user.UserId;
                x.Reason = reason;
            });

            connectionBridges.ForEach(x =>
            {
                x.LastModifiedBy = user.UserId;
                x.Reason = reason;
            });


            await _dbContext.SaveChangesAsync();

            //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            //Remove the records
            _dbContext.ConnectionBridge.RemoveRange(connectionBridges);
            _dbContext.DataType.RemoveRange(datatypes);
            _dbContext.DataSource.RemoveRange(datasources);
            _dbContext.Module.RemoveRange(modules);
            _dbContext.InProgressEvent.RemoveRange(activeEvents);
            _dbContext.ShareLinkSystemGenerated.RemoveRange(systemShareLinks);
            _dbContext.Site.Remove(site);

            await _dbContext.SaveChangesAsync();

            await PublishSiteAndDashboardTabDeleteMessageAsync(siteId, user.UserId.Value, false);
        }
        public async Task DeleteSiteRelatedEntitesAsync(int siteId, UserContextDTO user, bool cleanHistoryTables = false)
        {
            if (siteId == 0) return;

            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var cleanHistoryTablesConfiguration = _configuration.GetValue<bool>("CleanHistoryTables");
            cleanHistoryTables = cleanHistoryTables && cleanHistoryTablesConfiguration;

            var tables = new List<string> { "ShareLink", "ShareLinkAlias", "UserInfoBanner", "DashboardTab", "DashboardExport" };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            var dialect = _dbContext.GetDialect();
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            try
            {
                await dialect.LockAsync(_dbContext, 12345, token);
                await _dbContext.CleanDbEntries(siteId, cleanHistoryTables);
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await _dbContext.EnableTemporalTablesAsync(tables);
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await dialect.UnLockAsync(_dbContext, 12345, token);
            }

            await PublishSiteAndDashboardTabDeleteMessageAsync(siteId, user.UserId!.Value, cleanHistoryTables);
        }
        public async Task<List<SiteListItemDTO>> GetAllSitesAsync(UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                    || p.Permission == Permissions.ModuleIndependent.ADMIN
                    || p.Permission == Permissions.ModuleIndependent.VIEWER))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var result = await _dbContext.Site
                .Where(x => x.Id != 0)
                .Select(x => new SiteListItemDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    TimeZone = x.TimeZone,
                    IsEnabled = x.IsEnable,
                    SiteType = x.SiteType
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<List<PublicSiteDTO>> SearchPublicSiteDetailsAsync(SiteQueryDTO model)
        {
            var query = _dbContext.Site.AsQueryable();

            if (model.SiteId.Count > 0)
            {
                query = query.Where(x => model.SiteId.Contains(x.Id));
            }

            var result = await query
                .Where(x => x.Id != 0)
                .Select(x => new PublicSiteDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    TimeZone = x.TimeZone
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<List<SiteDTO>> SearchSiteDetailsAsync(SiteQueryDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.ModuleIndependent.VIEWER || p.Permission == Permissions.ModuleIndependent.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.NoUserAccess);
            }

            var query = _dbContext.Site.AsQueryable();

            if (model.SiteId.Count > 0)
            {
                query = query.Where(x => model.SiteId.Contains(x.Id));
            }

            if (model.SiteName.Count > 0)
            {
                query = query.Where(x => model.SiteName.Contains(x.Name));
            }

            if (model.ShortName.Count > 0)
            {
                query = query.Where(x => model.ShortName.Contains(x.ShortName));
            }

            var result = await query
                .Where(x => x.Id != 0)
                .Select(x => new SiteDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    TimeZone = x.TimeZone,
                    IsEnabled = x.IsEnable,
                    SiteType = x.SiteType
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<SiteConfigurationFormDTO> GetSingleSiteAsync(int siteId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var dbResult = await BuildSiteConfigurationFormAsync(siteId);

            if (dbResult == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            return dbResult;
        }

        [Obsolete]
        public async Task<SiteConfigurationFormDTO> GetSingleSiteLegacyAsync(int siteId)
        {
            var dbResult = await BuildSiteConfigurationFormAsync(siteId);

            if (dbResult == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            return dbResult;
        }

        private async Task<(List<DataSource> datasources2remove, Dictionary<int, List<Framework.Configuration.Model.Constants.Datasource.DataType>> datasourceTypeMap)>
            HandleDatasources(Site site, SiteUserConfigurationDTO model, UserContextDTO user, string key)
        {
            var datasources2remove = new List<DataSource>();
            var datasourceTypeMap = new Dictionary<int, List<Framework.Configuration.Model.Constants.Datasource.DataType>>();

            var dataTypesPipeline = new PluginPipeline<NoRequest, List<Framework.Configuration.Model.Constants.Datasource.DataType>>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var dataTypes = await dataTypesPipeline.RunOnAllTypesAsync<IDatasource>(
                (datasource, args, ct) => Task.FromResult(datasource.GetImplementedDataTypes()),
                args: Array.Empty<object>()
            );

            var connStringPipeline = new PluginPipeline<object[], object>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var groups = site.Datasources
                .Select(ds => new ConnectionContext<object[]>
                {
                    DatasourceId = ds.Id,
                    Request = new object[] { ds }
                })
                .ToList();

            await connStringPipeline.RunReduceAsync<IDatasource>(
                async (ds, args, ct) =>
                {
                    var datasource = (DataSource)args[0];

                    var updateModel = model.Datasources.FirstOrDefault(m => m.Id == datasource.Id);
                    var userId = user.UserId;
                    var changeReason = model.ChangeReason;
                    var encryptionKey = key;

                    if (updateModel == null)
                    {
                        datasource.LastModifiedBy = userId;
                        datasource.Reason = changeReason;
                        _dbContext.Entry(datasource).State = EntityState.Modified;
                        datasources2remove.Add(datasource);
                        return new object(); // dummy value
                    }

                    var types = dataTypes
                        .Where(d => d.ConnectionType == datasource.Type)
                        .SelectMany(d => d.Request)
                        .ToList();

                    datasourceTypeMap.Add(datasource.Id, types);

                    var existingModel = _dbContext.DataSource.FirstOrDefault(d => d.Id == datasource.Id);

                    if (existingModel == null)
                    {
                        throw new NullReferenceException("Datasource not found");
                    }

                    var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(existingModel.ConnectionString, key);

                    string encrypted = string.Empty;

                    try
                    {
                        var json = ds.TransformConnectionString(updateModel.ConnectionString, decryptedConnectionString);
                        var fixedJson = UnescapeSpecialChars(json);
                        await ds.ValidateConnectionString(fixedJson);
                        encrypted = ds.GetEncryptedConnectionStringAsync(json, encryptionKey);
                    }
                    catch (Exception ex)
                    {
                        throw new DCPBadRequestException($"{existingModel.Name} - {ex.Message}");
                    }

                    datasource.IsValidated = updateModel.IsValidated;
                    datasource.Name = updateModel.Name;
                    datasource.ConnectionString = encrypted;

                    if (_dbContext.Entry(datasource).State == EntityState.Modified)
                    {
                        datasource.LastModifiedBy = userId;
                        datasource.Reason = changeReason;
                    }

                    return new object(); // dummy value
                },
                groups: groups
            );

            var dataSourcesToRemoveIDs = datasources2remove.Select(x => x.Id).ToList();

            var updatedDataSources = site.Datasources.Where(x => !dataSourcesToRemoveIDs.Contains(x.Id)).ToList();

            await SaveDatasourceSecretAsync(updatedDataSources);

            //Add new DataSources
            var dataSources2add = model.Datasources
                .Where(d => d.Id == null)
                .ToList();

            if (dataSources2add.Count > 0)
            {
                await AddNewDatasourcesAsync(site, dataSources2add, user.UserId!.Value, model.ChangeReason);
            }

            return (datasources2remove, datasourceTypeMap);
        }

        private async Task<(List<Module> modules2remove, List<DataType> dataTypes2remove)> HandleModules(
            Site site,
            SiteUserConfigurationDTO model,
            UserContextDTO user, string key,
            Dictionary<int, List<Framework.Configuration.Model.Constants.Datasource.DataType>> datasourceTypeMap, List<RegisteredService> services)
        {
            //Handle Modules updates and deletes
            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);
            var modules2remove = new List<Module>();
            var datatypes2remove = new List<DataType>();

            foreach (var module in site.Modules)
            {
                var updateModel = model.Modules.FirstOrDefault(m => m.Id == module.Id);

                if (updateModel == null)
                {
                    //Maintain Audit trail
                    module.LastModifiedBy = user.UserId;
                    module.Reason = model.ChangeReason;
                    _dbContext.Entry(module).State = EntityState.Modified;
                    modules2remove.Add(module);
                    continue;
                }

                if (updateModel.IsEnabled)
                {
                    var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(module.ConnectionString, key);

                    ICalculationNode moduleInstance;

                    if (module.Type != updateModel.Type)
                    {
                        moduleInstance = calculationNodeFactory.LoadByType(updateModel.Type);
                    }
                    else
                    {
                        moduleInstance = calculationNodeFactory.LoadByConnectionString(module.Id, module.Type, decryptedConnectionString);
                    }

                    try
                    {
                        moduleInstance.UpdateConnectionString(updateModel.ConnectionString);
                        await moduleInstance.ValidateConnectionString();
                    }
                    catch (Exception ex)
                    {
                        throw new DCPBadRequestException($"{updateModel.Name} - {ex.Message}");
                    }

                    module.ConnectionString = moduleInstance.GetEncryptedConnectionStringAsync(key);

                    var existingModuleDataTypes = site.Datasources
                        .SelectMany(x => x.DataTypes)
                        .Where(x => string.Equals(
                            x.ModuleName,
                            module.Name,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    datatypes2remove.AddRange(await HandleDataTypes(updateModel, user, existingModuleDataTypes, module.Name, model.ChangeReason, datasourceTypeMap, services));

                    module.Name = updateModel.Name;
                    module.IsValidated = updateModel.IsValidated;

                    if (_dbContext.Entry(module).State == EntityState.Modified)
                    {
                        module.LastModifiedBy = user.UserId;
                        module.Reason = model.ChangeReason;
                    }
                }

                module.Reason = model.ChangeReason;
                module.IsEnable = updateModel.IsEnabled;
            }

            //Add new Modules
            var modules2add = model.Modules
                .Where(m => m.Id == null && m.IsEnabled)
                .ToList();

            if (modules2add.Count > 0)
            {
                await AddNewModulesAsync(site, modules2add, user.UserId!.Value, model.ChangeReason, services);
            }

            return (modules2remove, datatypes2remove);
        }

        private async Task<List<DataType>> HandleDataTypes(ModuleUserConfigurationDTO updateModel, UserContextDTO user,
            List<DAL.Entities.DataType> existingModuleDataTypes, string moduleName, string changeReason,
            Dictionary<int, List<Framework.Configuration.Model.Constants.Datasource.DataType>> datasourceTypeMap,
            List<RegisteredService> services)
        {
            //Validate DataType Mappings
            var moduleDataTypeSettings = services
                .Where(x => string.Equals(
                    x.MetaData.ModuleName,
                    updateModel.Name,
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.MetaData.DataTypes)
                .FirstOrDefault();

            var dataTypes2remove = new List<DataType>();

            if (moduleDataTypeSettings == null)
            {
                throw new DCPBadRequestException($"{moduleName} - " + ErrorMessages.UnknownRequiredDataType);
            }

            foreach (var datatype in updateModel.DataTypes)
            {
                if (!datasourceTypeMap[datatype.DatasourceId].Contains(datatype.DataType))
                {
                    throw new DCPBadRequestException($"{moduleName} - " + ErrorMessages.InvalidDatasourceDataTypeMapping);
                }
            }

            foreach (var dataTypeSetting in moduleDataTypeSettings)
            {
                if (dataTypeSetting.Mandatory
                    && !updateModel.DataTypes.Any(dt => dt.DataType == dataTypeSetting.Type))
                {
                    throw new DCPBadRequestException($"{moduleName} - " + ErrorMessages.MissingRequiredDataType);
                }

                if (!dataTypeSetting.AllowMultiselection && updateModel.DataTypes.Count(x => x.DataType == dataTypeSetting.Type) > 1)
                {
                    throw new DCPBadRequestException($"{moduleName} - " + ErrorMessages.InvalidMultiDataTypeMapping);
                }
            }

            var moduleDataTypes2add = updateModel.DataTypes
                .Where(x => x.Id == null)
                .Select(x => new DataType
                {
                    DataSourceId = x.DatasourceId,
                    LastModifiedBy = user.UserId,
                    OwnerUserId = user.UserId,
                    Reason = changeReason,
                    ModuleName = moduleName,
                    Type = x.DataType
                })
                .ToList();

            foreach (var dataType in existingModuleDataTypes)
            {
                var updateDataType = updateModel.DataTypes.FirstOrDefault(dt => dt.Id == dataType.Id);

                if (updateDataType == null)
                {
                    //Maintain Audit trail
                    dataType.LastModifiedBy = user.UserId;
                    dataType.Reason = changeReason;
                    _dbContext.Entry(dataType).State = EntityState.Modified;
                    dataTypes2remove.Add(dataType);
                    continue;
                }

                dataType.DataSourceId = updateDataType.DatasourceId;
                dataType.Type = updateDataType.DataType;
                dataType.ModuleName = moduleName;

                if (_dbContext.Entry(dataType).State == EntityState.Modified)
                {
                    dataType.LastModifiedBy = user.UserId;
                    dataType.Reason = changeReason;
                }
            }

            await _dbContext.DataType.AddRangeAsync(moduleDataTypes2add);

            return dataTypes2remove;
        }

        private async Task<List<ConnectionBridge>> HandleConnectionBridges(Dictionary<int, DataSource> datasources, SiteUserConfigurationDTO model, UserContextDTO user)
        {
            var connectionBridges2remove = new List<ConnectionBridge>();

            var datasourceIds = datasources.Select(d => d.Key).ToList();

            var connectionBridges = await _dbContext.ConnectionBridge.Where(cb =>
            datasourceIds.Contains(cb.TConId) && datasourceIds.Contains(cb.SConId)).ToListAsync();

            foreach (var connectionBridge in connectionBridges)
            {
                var updateModel = model.ConnectionBridges.FirstOrDefault(cb => cb.Id == connectionBridge.Id);

                if (updateModel == null)
                {
                    //Maintain Audit trail
                    connectionBridge.LastModifiedBy = user.UserId;
                    connectionBridge.Reason = model.ChangeReason;
                    _dbContext.Entry(connectionBridge).State = EntityState.Modified;
                    connectionBridges2remove.Add(connectionBridge);
                    continue;
                }

                if (!Enum.TryParse<ConnectionBridgeStrategyType>(updateModel.Type, ignoreCase: true, out var strategy))
                {
                    throw new DCPBadRequestException($"Incorrect strategy type: {updateModel.Type}");
                }

                connectionBridge.Name = updateModel.Name;
                connectionBridge.SConId = updateModel.SConnectionId;
                connectionBridge.SCon = datasources[updateModel.SConnectionId];
                connectionBridge.TConId = updateModel.TConnectionId;
                connectionBridge.TCon = datasources[updateModel.TConnectionId];
                connectionBridge.SConDataType = (byte)updateModel.SConnectionType;
                connectionBridge.TConDataType = (byte)updateModel.TConnectionType;

                var definition = ConnectionBridgeStrategyFactory.LoadByType(strategy);
                definition.LoadFromForm(updateModel.StrategyDefintion);
                connectionBridge.Definition = definition;

                if (_dbContext.Entry(connectionBridge).State == EntityState.Modified)
                {
                    connectionBridge.LastModifiedBy = user.UserId;
                    connectionBridge.Reason = model.ChangeReason;
                }
            }

            //Add new Connection bridges
            var connectionBridges2add = model.ConnectionBridges
                .Where(m => m.Id == null)
                .ToList();

            if (connectionBridges2add.Count > 0)
            {
                await AddNewConnectionBridgesAsync(datasources, connectionBridges2add, user.UserId!.Value, model.ChangeReason, connectionBridges);
            }

            return connectionBridges2remove;
        }

        public async Task<SiteConfigurationFormDTO> UpdateSiteAsync(int siteId, SiteUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var site = await _dbContext.Site
                .Where(x => x.Id == siteId && siteId != 0)
                .Include(x => x.Datasources)
                .ThenInclude(x => x.DataTypes)
                .Include(x => x.Modules)
                .FirstOrDefaultAsync();

            if (site == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            if (await _dbContext.Site.AnyAsync(x => x.Name == model.Name && x.Id != siteId))
            {
                throw new DCPBadRequestException(ErrorMessages.SiteNameExist);
            }

            var key = GetEncryptionKey();
            var env = _configuration["Host:EnvironmentSlug"];

            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend" }, env);

            var moduleNames = services
                .Select(s => s.MetaData?.ModuleName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (model.Modules.Any(m => !moduleNames.Contains(m.Name)))
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            //Update Site properties
            site.SiteType = model.Type;
            site.Name = model.Name;
            site.IsEnable = model.IsEnabled;
            site.TimeZone = model.TimeZone;
            site.ShortName = model.ShortName;

            if (_dbContext.Entry(site).State == EntityState.Modified)
            {
                site.LastModifiedBy = user.UserId;
                site.Reason = model.ChangeReason;
            }

            //Handle Datasources add, updates and deletes
            var datasourceHandleResult = await HandleDatasources(site, model, user, key);

            //Handle Modules add, updates and deletes
            var moduleHandleResult = await HandleModules(site, model, user, key, datasourceHandleResult.datasourceTypeMap, services);

            var datasources = site.Datasources.Where(d => d.Id != 0)
                .ToDictionary(
                    d => d.Id,
                    d => d
                );

            //Handle Connection bridges add, updates and deletes
            var connectionBridges2remove = await HandleConnectionBridges(datasources, model, user);

            await _dbContext.SaveChangesAsync();

            if (moduleHandleResult.modules2remove.Count > 0
                || datasourceHandleResult.datasources2remove.Count > 0
                || moduleHandleResult.dataTypes2remove.Count > 0
                || connectionBridges2remove.Count > 0)
            {
                //Add delay to have audit trail correct
                await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

                _dbContext.ConnectionBridge.RemoveRange(connectionBridges2remove);
                _dbContext.DataSource.RemoveRange(datasourceHandleResult.datasources2remove);
                _dbContext.Module.RemoveRange(moduleHandleResult.modules2remove);
                _dbContext.DataType.RemoveRange(moduleHandleResult.dataTypes2remove);

                await _dbContext.SaveChangesAsync();
            }

            return await BuildSiteConfigurationFormAsync(siteId);
        }
        private async Task AddNewModulesAsync(Site site, List<ModuleUserConfigurationDTO> modules, int userId, string changeReason, List<RegisteredService> services)
        {
            //Modules
            var key = GetEncryptionKey();
            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);

            var dbModules = modules
               .Select(x => new
               {
                   Module = calculationNodeFactory.LoadByType(x.Type),
                   UserConfiguration = x
               })
               .ToList();

            var modules2Add = new List<Module>();
            var moduleDataTypes2add = new List<DataType>();

            foreach (var module in dbModules)
            {
                try
                {
                    module.Module.UpdateConnectionString(module.UserConfiguration.ConnectionString);
                    await module.Module.ValidateConnectionString();
                }
                catch (Exception ex)
                {
                    throw new DCPBadRequestException($"{module.UserConfiguration.Name} - {ex.Message}");
                }

                //validation for dataType mappings 
                var moduleDataTypeSettings = services
                .Where(x => string.Equals(
                    x.MetaData.ModuleName,
                    module.UserConfiguration.Name,
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.MetaData.DataTypes)
                .FirstOrDefault();

                if (moduleDataTypeSettings == null)
                {
                    throw new DCPBadRequestException($"{module.UserConfiguration.Name} - " + ErrorMessages.UnknownRequiredDataType);
                }

                foreach (var dataTypeSetting in moduleDataTypeSettings)
                {
                    if (dataTypeSetting.Mandatory
                        && !module.UserConfiguration.DataTypes.Any(dt => dt.DataType == dataTypeSetting.Type))
                    {
                        throw new DCPBadRequestException($"{module.UserConfiguration.Name} - " + ErrorMessages.MissingRequiredDataType);
                    }

                    if (!dataTypeSetting.AllowMultiselection && module.UserConfiguration.DataTypes.Count(x => x.DataType == dataTypeSetting.Type) > 1)
                    {
                        throw new DCPBadRequestException($"{module.UserConfiguration.Name} - " + ErrorMessages.InvalidMultiDataTypeMapping);
                    }
                }

                //creation of dataTypes
                var moduleDataTypes = module.UserConfiguration.DataTypes
                    .Where(x => x.Id == null)
                    .Select(x => new DataType
                    {
                        DataSourceId = x.DatasourceId,
                        LastModifiedBy = userId,
                        OwnerUserId = userId,
                        Reason = changeReason,
                        ModuleName = module.UserConfiguration.Name,
                        Type = x.DataType
                    })
                    .ToList();

                moduleDataTypes2add.AddRange(moduleDataTypes);

                var module2add = new Module
                {
                    ConnectionString = module.Module.GetEncryptedConnectionStringAsync(key),
                    IsValidated = module.UserConfiguration.IsValidated,
                    LastModifiedBy = userId,
                    IsEnable = module.UserConfiguration.IsEnabled,
                    Name = module.UserConfiguration.Name,
                    Reason = changeReason,
                    Site = site,
                    OwnerUserId = userId,
                    Type = module.UserConfiguration.Type
                };

                modules2Add.Add(module2add);
            }

            await _dbContext.DataType.AddRangeAsync(moduleDataTypes2add);
            await _dbContext.Module.AddRangeAsync(modules2Add);
        }

        private async Task AddNewDatasourcesAsync(Site site, List<DatasourceUserConfigurationDTO> datasources, int userId, string changeReason)
        {
            var key = GetEncryptionKey();
            var datasources2Add = new List<DataSource>();

            var connStringPipeline = new PluginPipeline<object[], string>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            await Task.WhenAll(
                datasources.Select(async x =>
                {
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
                                throw new DCPBadRequestException($"{x.Name} - {ex.Message}");
                            }

                            return encrypted;
                        },
                        connectionType: x.Type,
                        args: new object[] { x.ConnectionString }
                    );

                    var datasource2add = new DataSource
                    {
                        ConnectionString = encrypted,
                        IsValidated = x.IsValidated,
                        LastModifiedBy = userId,
                        Name = x.Name,
                        Reason = changeReason,
                        Site = site,
                        OwnerUserId = userId,
                        Type = x.Type
                    };

                    datasources2Add.Add(datasource2add);
                })
            );

            await _dbContext.AddRangeAsync(datasources2Add);
            await _dbContext.SaveChangesAsync();

            await SaveDatasourceSecretAsync(datasources2Add);
        }

        private async Task AddNewConnectionBridgesAsync(
            Dictionary<int, DataSource> datasources,
            List<ConnectionBridgeUserConfigurationForm> connectionBridges,
            int userId,
            string changeReason,
            List<ConnectionBridge> existingConnectionBridges)
        {
            var connectionBridges2Add = new List<ConnectionBridge>();

            var existingSet = new HashSet<(int, int, byte, byte)>(
                existingConnectionBridges.Select(e =>
                    (e.SConId, e.TConId, e.SConDataType, e.TConDataType))
            );

            foreach (var connectionBridge in connectionBridges)
            {
                var tuple = (
                    connectionBridge.SConnectionId,
                    connectionBridge.TConnectionId,
                    (byte)connectionBridge.SConnectionType,
                    (byte)connectionBridge.TConnectionType
                );

                if (!existingSet.Add(tuple))
                {
                    throw new DCPBadRequestException(ErrorMessages.InvalidConnectionBridges);
                }

                var connBridge2add = new ConnectionBridge
                {
                    Name = connectionBridge.Name,
                    SConId = connectionBridge.SConnectionId,
                    SCon = datasources[connectionBridge.SConnectionId],
                    TConId = connectionBridge.TConnectionId,
                    TCon = datasources[connectionBridge.TConnectionId],
                    SConDataType = (byte)connectionBridge.SConnectionType,
                    TConDataType = (byte)connectionBridge.TConnectionType,
                    LastModifiedBy = userId,
                    OwnerUserId = userId,
                    Reason = changeReason,
                };

                if (!Enum.TryParse<ConnectionBridgeStrategyType>(connectionBridge.Type, ignoreCase: true, out var strategy))
                {
                    throw new DCPBadRequestException($"Incorrect strategy type: {connectionBridge.Type}");
                }

                var definition = ConnectionBridgeStrategyFactory.LoadByType(strategy);
                definition.LoadFromForm(connectionBridge.StrategyDefintion);
                connBridge2add.Definition = definition;

                connectionBridges2Add.Add(connBridge2add);
            }

            await _dbContext.ConnectionBridge.AddRangeAsync(connectionBridges2Add);
        }

        private async Task<SiteConfigurationFormDTO> BuildSiteConfigurationFormAsync(int siteId)
        {
            var dbResult = await _dbContext.Site
                .Where(x => x.Id == siteId && x.Id != 0)
                .Include(x => x.Datasources)
                .Include(x => x.Modules)
                .Select(x => new SiteConfigurationFormDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    TimeZone = x.TimeZone,
                    IsEnabled = x.IsEnable,
                    Type = x.SiteType,
                    Modules = x.Modules
                        .Select(m => new ModuleConfigurationForm
                        {
                            Id = m.Id,
                            IsEnabled = m.IsEnable,
                            IsValidated = m.IsValidated,
                            Name = m.Name,
                            Type = m.Type,
                        }).ToList(),
                    Datasources = x.Datasources
                                .Select(d => new DatasourceConfigurationForm
                                {
                                    Id = d.Id,
                                    IsValidated = d.IsValidated,
                                    Name = d.Name,
                                    Type = d.Type,
                                }).ToList()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (dbResult == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            var dataTypes = await _dbContext.DataType
                .Where(x => x.DataSource!.SiteId == siteId)
                .Select(x => new
                {
                    x.Id,
                    x.DataSourceId,
                    x.Type,
                    x.ModuleName
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var module in dbResult.Modules)
            {
                module.DataTypes = dataTypes
                    .Where(x => x.ModuleName == module.Name)
                    .Select(x => new DataTypeConfigurationFormDTO
                    {
                        DatasourceId = x.DataSourceId,
                        Id = x.Id,
                        DataType = x.Type
                    }).ToList();
            }

            var datasourceIds = dbResult.Datasources.Select(x => x.Id).ToList();

            var connectionBridges = await _dbContext.ConnectionBridge
                .Where(cb => datasourceIds.Contains(cb.SConId) && datasourceIds.Contains(cb.TConId))
                .ToListAsync();

            dbResult.ConnectionBridges = connectionBridges.Select(cb => new ConnectionBridgeConfigurationForm
            {
                Id = cb.Id,
                Name = cb.Name,
                SConnectionId = cb.SConId,
                SConnectionType = (Framework.Configuration.Model.Constants.Datasource.DataType)cb.SConDataType,
                TConnectionId = cb.TConId,
                TConnectionType = (Framework.Configuration.Model.Constants.Datasource.DataType)cb.TConDataType,
                Type = cb.Strategy.ToString()
            }).ToList();


            return dbResult;
        }

        public async Task<List<AuditEntryDisplayDTO>> GetSiteAuditTrailAsync(int siteId, UserContextDTO user)
        {
            if (siteId == 0) return [];

            var auditHelper = new AuditHelper(_dbContext);

            //Get the changes
            var changes = await auditHelper.GetAuditForTablesAsync(
                startTime: DateTime.MinValue,
                endTime: DateTime.MaxValue,
                recordId: null,
                siteId: siteId,
                new List<Type> {
                    typeof(Site),
                    typeof(Module),
                    typeof(DataSource),
                    typeof(DataType),
                    typeof(ConnectionBridge)
                });

            //For the user mapping
            var users = await _dbContext.User
                .Select(x => new
                {
                    x.UnixId,
                    x.Id
                })
                .AsNoTracking()
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.UnixId
                );

            return changes.Select(x => new AuditEntryDisplayDTO
            {
                ActionType = x.ActionType,
                FieldName = x.FieldName,
                NewValue = x.NewValue,
                OldValue = x.OldValue,
                Reason = x.Reason,
                TableName = x.TableName,
                RecordId = x.RecordId,
                UserId = x.UserId,
                Timestamp = x.Timestamp,
                UserName = x.UserId.HasValue ? x.UserId.Value == 0 ? "System" : users[x.UserId.Value] : "N/A"
            }).ToList();
        }

        public async Task<List<ModuleListItemDTO>> GetSiteModulesAsync(int siteId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.SiteId == siteId))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var sites = await _dbContext.Module
                .Where(x => x.SiteId == siteId)
                .Select(x => new ModuleListItemDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsEnabled = x.IsEnable
                })
                .AsNoTracking()
                .ToListAsync();

            return sites;
        }

        private async Task PublishSiteAndDashboardTabDeleteMessageAsync(int siteId, int userId, bool cleanHistoryTables)
        {
            var message = new DeleteSiteMessage
            {
                CleanHistoryTables = cleanHistoryTables,
                CurrentUserId = userId,
                Id = siteId,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };

            var jMessage = JsonDocument.Parse(JsonSerializer.Serialize(message)).RootElement;

            _messageBus.Publish(
                message: jMessage,
                exchangerName: MessageExchangers.EventBus,
                keys: new List<string> { RoutingKeys.SiteDelete }
                );

            //To be removed after migration
            _messageBus.Publish(
                message: jMessage,
                exchangerName: MessageExchangers.LegacyEventBus,
                keys: new List<string> { RoutingKeys.SiteDelete }
            );

            var dashboardTabs = _dbContext.DashboardTab.Where(x => x.SiteId == siteId);

            await dashboardTabs
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.LastModifiedBy, userId));

            var dashboardTabsForDelete = await dashboardTabs.ToListAsync();

            var dashboardTabIds = dashboardTabsForDelete.Select(x => x.Id).Distinct();

            var dashboardExports = _dbContext.DashboardExport
                .Where(x => dashboardTabIds.Contains(x.DashboardTabId));

            await dashboardExports
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.LastModifiedBy, userId));

            var dashboardExportsForDelete = await dashboardExports.ToListAsync();

            //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.DashboardExport.RemoveRange(dashboardExportsForDelete);
            _dbContext.DashboardTab.RemoveRange(dashboardTabsForDelete);

            await _dbContext.SaveChangesAsync();

            //send request to Rabbit MQ that dashboard tab is deleted
            //Use this requestModel for request with key DashboardTabsDeleteRoutingKey
            bool isGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false);
            var requestModel = dashboardTabsForDelete.Select(tab => new DashboardTabDTO
            {
                SiteId = tab.SiteId,
                Id = tab.Id,
                Name = tab.Name,
                LastModifiedBy = userId,
                IsGxp = isGxp
            }).ToList();

            var jMessageTabs = JsonDocument.Parse(JsonSerializer.Serialize(requestModel)).RootElement;

            _messageBus.Publish(
                message: jMessageTabs,
                exchangerName: MessageExchangers.EventBus,
                keys: new List<string> { RoutingKeys.DashboardTabsDelete }
                );

            var requestModelLegacy = requestModel.Select(d => new DashboardTabLegacyDTO
            {
                ID = d.Id,
                Name = d.Name,
                SiteId = d.SiteId,
                DateUpdated = d.LastModifiedAt,
                UserIDCreated = d.OwnerUserId,
                UserIDUpdated = d.LastModifiedBy,
                CurrentUserId = d.LastModifiedBy.Value,
                IsGxp = d.IsGxp
            }).ToList();

            var jMessageLegacy = JsonDocument.Parse(JsonSerializer.Serialize(requestModelLegacy)).RootElement;

            _messageBus.Publish(
                message: jMessageLegacy,
                exchangerName: MessageExchangers.LegacyEventBus,
                keys: new List<string> { RoutingKeys.DashboardTabsDelete }
                );
        }

        private async Task SaveDatasourceSecretAsync(List<DataSource> dataSources)
        {
            var key = GetEncryptionKey();

            foreach (var ds in dataSources)
            {
                var relativePath = $"{_configuration["Host:EnvironmentSlug"]}/{ds.Id}";

                var decryptedJson = EncryptionHelper.DecryptConnectionString(ds.ConnectionString, key);

                var secret = JsonSerializer.Deserialize<Dictionary<string, object>>(decryptedJson);

                await _configProvider.SaveSecretAsync(relativePath, secret);
            }
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
    }
}

namespace OpenDCP.Framework.Configuration.Model
{
    public class ModuleDataTypeConfigurationDTO
    {
        public bool Mandatory { get; set; }
        public bool AllowMultiselection { get; set; }

        // Typ muss mit dem in eurem Projekt verwendeten enum übereinstimmen.
        // In eurem Code wird Framework.Configuration.Model.Constants.Datasource.Data.Type verwendet:
        public Framework.Configuration.Model.Constants.Datasource.DataType Type { get; set; }
    }
}
