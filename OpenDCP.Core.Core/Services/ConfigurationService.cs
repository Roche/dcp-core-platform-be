using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenDCP.Core.Core.Classes;
using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.ConnectionBridge;
using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Constants;
using OpenDCP.Framework.Connections.Manager.Factories;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Models.StrategyDefintions;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Datasource.Base.Interfaces;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ConnectionBridgeDTO = OpenDCP.Framework.Connections.Manager.Models.ConnectionBridgeDTO;

namespace OpenDCP.Core.Core.Services
{
    public class ConfigurationService : BaseService, IConfigurationService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;
        protected DbConnectionStringLoader _loader;
        public ConfigurationService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache, IServiceDiscovery serviceDiscovery, IServiceProvider serviceProvider, DatasourceCache cache) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _serviceProvider = serviceProvider;
            _cache = cache;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<DatasourceConfigurationDTO> GetDatasourceConfigurationByIdAsync(int datasourceId, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, 0, true);

            var key = GetEncryptionKey();

            var dbResult = await _dbContext.DataSource
                .Where(x => x.Id == datasourceId)
                .Select(x => new
                {
                    Id = x.Id,
                    x.Name,
                    DataTypes = x.DataTypes.Select(x => x.Type).Distinct().ToList(),
                    ConnectionType = x.Type,
                    SiteId = x.SiteId,
                    ConnectionString = x.ConnectionString,
                    IsValidated = x.IsValidated
                }
                ).AsNoTracking()
                .FirstOrDefaultAsync();

            if (dbResult == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownSite);
            }

            var result = new DatasourceConfigurationDTO
            {
                Id = dbResult.Id,
                Name = dbResult.Name,
                DataTypes = dbResult.DataTypes,
                ConnectionType = dbResult.ConnectionType,
                SiteId = dbResult.SiteId,
                ConnectionString = EncryptionHelper.DecryptConnectionString(dbResult.ConnectionString, key),
                IsValidated = dbResult.IsValidated
            };

            return result;
        }

        public async Task<List<InputField>> GetDatasourceConnectionStringConfigurationFormAsync(string type, int? datasourceId, UserContextDTO user)
        {
            List<InputField> configurationForm;

            var pipeline = new PluginPipeline<NoRequest, List<InputField>>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            if (datasourceId == null)
            {
                configurationForm = await pipeline.RunOnConnectionTypeAsync<IDatasource>(
                (datasource, args, ct) => Task.FromResult(datasource.GetConnectionStringConfigurationForm()),
                connectionType: type,
                args: Array.Empty<object>()
            );
            }
            else
            {
                configurationForm = await pipeline.RunReduceAsync<IDatasource>(
                (datasource, args, ct) => Task.FromResult(datasource.GetConnectionStringConfigurationForm()),
                groups: new List<ConnectionContext<NoRequest>>
                {
                        new ConnectionContext<NoRequest>
                        {
                            DatasourceId = datasourceId.Value,
                            Request = new NoRequest()
                        }
                }
            );
            }

            return configurationForm;
        }

        public async Task<ModuleConfigurationDTO> GetModuleConfigurationByIdAsync(int moduleId, UserContextDTO user)
        {
            var key = GetEncryptionKey();

            var dbResult = await _dbContext.Module
               .Where(x => x.Id == moduleId)
               .Select(x => new
               {
                   Id = x.Id,
                   Name = x.Name,
                   ConnectionType = x.Type,
                   SiteId = x.SiteId,
                   ConnectionString = x.ConnectionString,
                   IsValidated = x.IsValidated,
                   IsEnable = x.IsEnable
               }
               ).AsNoTracking()
               .FirstOrDefaultAsync();

            if (dbResult == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            var result = new ModuleConfigurationDTO
            {
                Id = dbResult.Id,
                Name = dbResult.Name,
                CalculationNodeType = dbResult.ConnectionType,
                ConnectionString = EncryptionHelper.DecryptConnectionString(dbResult.ConnectionString, key),
                SiteId = dbResult.SiteId,
                IsValidated = dbResult.IsValidated,
                IsEnabled = dbResult.IsEnable
            };

            return result;
        }

        public async Task<List<InputField>> GetModuleConnectionStringConfigurationFormAsync(CalculationNodeType type, int? moduleId, UserContextDTO user)
        {
            var calculatioNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);

            ICalculationNode calculationNode;

            if (moduleId == null)
            {
                calculationNode = calculatioNodeFactory.LoadByType(type);
            }
            else
            {
                calculationNode = await calculatioNodeFactory.LoadByIdAsync(moduleId.Value, type);
            }

            return calculationNode.GetConnectionStringConfigurationForm();
        }

        public async Task<SystemConfigurationDTO> GetActiveSystemConfigurationAsync(UserContextDTO user)
        {
            var env = _configuration["Host:EnvironmentSlug"];
            var queryTags = new List<string> { "backend", "api" };
            var services = await _serviceDiscovery.QueryActiveServices(queryTags, env);

            var modules = services
                .Distinct()
                .Select(x =>
                new ModuleSystemConfigurationDTO
                {
                    Name = x.MetaData.ModuleName,
                    IsGxP = x.Tags.Contains("gxp"),
                    IsGlobal = x.MetaData.IsGlobal,
                    DataTypes = x.MetaData.DataTypes.Select(dt => new RequiredDataTypeDTO
                    {
                        AllowMultiselection = dt.AllowMultiselection,
                        Mandatory = dt.Mandatory,
                        Type = dt.Type,
                    }).ToList(),
                    Resources = x.MetaData.Resources.Select(r => new ModuleResourceDTO
                    {
                        Type = r.Type,
                        Url = r.ResourceURL
                    }).ToList(),
                })
               .ToList();

            var pipeline = new PluginPipeline<NoRequest, List<Framework.Configuration.Model.Constants.Datasource.DataType>>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var dataTypes = await pipeline.RunOnAllTypesAsync<IDatasource>(
                (datasource, args, ct) => Task.FromResult(datasource.GetImplementedDataTypes()),
                args: Array.Empty<object>()
            );

            var result = new SystemConfigurationDTO
            {
                Modules = modules,
                CalculationNodeTypes = Enum.GetValues(typeof(CalculationNodeType))
                                            .Cast<CalculationNodeType>()
                                            .Select(type => new InputFieldOption
                                            {
                                                Label = type.ToString(),
                                                Value = type.ToString(),

                                            })
                                            .ToList(),
                DatasourceConnectionTypes = _cache.GetPluginConnectionTypes()
                                            .Select(type =>
                                               new DatasourceConnectionTypeDTO
                                               {
                                                   Label = type.ToString(),
                                                   Value = type,
                                                   AvailableDataTypes = dataTypes
                                                                        .Where(d => d.ConnectionType == type)
                                                                        .SelectMany(d => d.Request).ToList()
                                               }
                                            )
                                            .ToList(),
                ConnectionBridgeTypes = Enum.GetValues(typeof(ConnectionBridgeStrategyType))
                                            .Cast<ConnectionBridgeStrategyType>()
                                            .Select(type => new InputFieldOption
                                            {
                                                Label = type.ToString(),
                                                Value = type.ToString(),

                                            })
                                            .ToList(),
                DataTypes = Enum.GetValues(typeof(Framework.Configuration.Model.Constants.Datasource.DataType))
                                            .Cast<Framework.Configuration.Model.Constants.Datasource.DataType>()
                                            .Select(type => new InputFieldOption
                                            {
                                                Label = type.ToString(),
                                                Value = type.ToString(),

                                            })
                                            .ToList(),
            };

            return result;
        }

        public async Task<List<DatasourceConfigurationDTO>> SearchDatasourceConfigurationAsync(Models.Models.Configuration.DatasourceQueryDTO model, UserContextDTO user)
        {
            if (model.DataType.Count > 1 && model.SiteId.Count > 1)
            {
                throw new DCPBadRequestException(ErrorMessages.UnallowedSearchParameterCardinality);
            }

            var key = GetEncryptionKey();

            var query = _dbContext.DataSource.AsQueryable();

            if (model.SiteId.Count > 0)
            {
                query = query.Where(ds => model.SiteId.Contains(ds.SiteId));
            }

            if (!string.IsNullOrEmpty(model.Module) && model.DataType.Count > 0)
            {
                query = query.Where(x =>
                    x.DataTypes.Any(dt =>
                        dt.ModuleName == model.Module &&
                        model.DataType.Contains(dt.Type)));
            }
            else
            {
                if (!string.IsNullOrEmpty(model.Module))
                {
                    query = query.Where(x => x.DataTypes.Any(dt => dt.ModuleName == model.Module));
                }

                if (model.DataType.Count > 0)
                {
                    query = query.Where(x => x.DataTypes.Any(dt => model.DataType.Contains(dt.Type)));
                }
            }

            var dbResult = await query
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    DataTypes = x.DataTypes.Select(dt => dt.Type).Distinct().ToList(),
                    ConnectionType = x.Type,
                    SiteId = x.SiteId,
                    ConnectionString = x.ConnectionString,
                    IsValidated = x.IsValidated
                }
                )
                .AsNoTracking()
                .ToListAsync();

            var result = dbResult
                .Select(x => new DatasourceConfigurationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    DataTypes = x.DataTypes,
                    ConnectionType = x.ConnectionType,
                    SiteId = x.SiteId,
                    ConnectionString = EncryptionHelper.DecryptConnectionString(x.ConnectionString, key),
                    IsValidated = x.IsValidated,
                }
                ).ToList();

            return result;
        }

        public async Task<List<ModuleConfigurationDTO>> SearchModuleConfigurationAsync(Models.Models.Configuration.ModuleQueryDTO model, UserContextDTO user)
        {
            var key = GetEncryptionKey();

            var query = _dbContext.Module.AsQueryable();

            if (!string.IsNullOrEmpty(model.Module))
            {
                query = query.Where(x => x.Name == model.Module);
            }

            if (model.SiteId.Count > 0)
            {
                query = query.Where(x => model.SiteId.Contains(x.SiteId));
            }

            if (!model.IncludeDisabledModules)
            {
                query = query.Where(x => x.IsEnable);
            }

            var dbResult = await query
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    ConnectionType = x.Type,
                    SiteId = x.SiteId,
                    ConnectionString = x.ConnectionString,
                    x.IsValidated,
                    x.IsEnable
                }
                ).AsNoTracking()
                .ToListAsync();

            var result = dbResult
                .Select(x => new ModuleConfigurationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    CalculationNodeType = x.ConnectionType,
                    SiteId = x.SiteId,
                    ConnectionString = EncryptionHelper.DecryptConnectionString(x.ConnectionString, key),
                    IsValidated = x.IsValidated,
                    IsEnabled = x.IsEnable,
                }
                ).ToList();

            return result;
        }

        public async Task<List<ApiServiceConfigurationDTO>> ReloadApiGatewayStatusAsync(List<ApiServiceConfigurationDTO> model, UserContextDTO user, string reason)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var apiGateway = new ExternalGatewayManagement(_configuration, _serviceDiscovery);

            var parameters = new JsonObject
            {
                ["GatewayAPIConfiguration"] = JsonSerializer.SerializeToNode(model),
                ["Reason"] = reason
            };

            string configuration = parameters.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var auditTrailEntry = new AuditTrailAdminAction
            {
                ActionConfiguration = configuration,
                Exception = string.Empty,
                Module = ModulesType.APIGateway,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                UserId = user.UserId,
            };

            _dbContext.AuditTrailAdminAction.Add(auditTrailEntry);

            await _dbContext.SaveChangesAsync();

            return await apiGateway.ReloadStatusAsync(model);
        }

        public Task<List<ApiServiceConfigurationDTO>> GetApiGatewayStatusAsync(UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var apiGateway = new ExternalGatewayManagement(_configuration, _serviceDiscovery);
            return apiGateway.GetStatusAsync();
        }

        public async Task<List<ApiServiceConfigurationDTO>> SetApiGatewayStatusAsync(List<ApiServiceConfigurationDTO> model, UserContextDTO user, string reason)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var apiGateway = new ExternalGatewayManagement(_configuration, _serviceDiscovery);

            var parameters = new JsonObject
            {
                ["GatewayAPIConfiguration"] = JsonSerializer.SerializeToNode(model),
                ["Reason"] = reason
            };

            string configuration = parameters.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var auditTrailEntry = new AuditTrailAdminAction
            {
                ActionConfiguration = configuration,
                Exception = string.Empty,
                Module = ModulesType.APIGateway,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                UserId = user.UserId,
            };

            _dbContext.AuditTrailAdminAction.Add(auditTrailEntry);

            await _dbContext.SaveChangesAsync();

            return await apiGateway.SetStatusAsync(model);
        }

        public async Task<List<DatasourcePublicConfigurationDTO>> PublicSearchDatasourceConfigurationAsync(Models.Models.Configuration.DatasourceQueryDTO model, UserContextDTO user)
        {
            var query = _dbContext.DataSource.AsQueryable();

            if (model.SiteId.Count > 0)
            {
                query = query.Where(ds => model.SiteId.Contains(ds.SiteId));
            }

            if (!string.IsNullOrEmpty(model.Module) && model.DataType.Count > 0)
            {
                query = query.Where(x =>
                    x.DataTypes.Any(dt =>
                        dt.ModuleName == model.Module &&
                        model.DataType.Contains(dt.Type)));
            }
            else
            {
                if (!string.IsNullOrEmpty(model.Module))
                {
                    query = query.Where(x => x.DataTypes.Any(dt => dt.ModuleName == model.Module));
                }

                if (model.DataType.Count > 0)
                {
                    query = query.Where(x => x.DataTypes.Any(dt => model.DataType.Contains(dt.Type)));
                }
            }

            var dbResult = await query
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    DataTypes = x.DataTypes.Select(dt => dt.Type).Distinct().ToList(),
                    SiteName = x.Site.Name,
                    ConnectionType = x.Type,
                    SiteId = x.SiteId,
                    ConnectionString = x.ConnectionString,
                    IsValidated = x.IsValidated
                }
                )
                .AsNoTracking()
                .ToListAsync();

            var result = dbResult
                .Select(x => new DatasourcePublicConfigurationDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    DataTypes = x.DataTypes,
                    ConnectionType = x.ConnectionType,
                    SiteId = x.SiteId,
                    SiteName = x.SiteName,
                    IsValidated = x.IsValidated,
                }
                ).ToList();

            return result;
        }

        public async Task<List<InputField>> GetConnectionBridgeConfigurationFormAsync(ConnectionBridgeStrategyType type, int? connectionBridgeId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            StrategyDefintion? strategy;

            if (connectionBridgeId is null)
            {
                strategy = ConnectionBridgeStrategyFactory.LoadByType(type);
            }
            else
            {
                var strategyFromDb = await _dbContext.ConnectionBridge
                    .Where(x => x.Id == connectionBridgeId.Value)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (strategyFromDb is null)
                    throw new DCPNotFoundException($"Connection bridge with id: {connectionBridgeId} not found");

                strategy = strategyFromDb.Definition;

                if (strategy is null)
                    throw new DCPNotFoundException($"Connection bridge with id: {connectionBridgeId} not found");
            }

            return strategy.Form();
        }

        public async Task<List<ConnectionBridgeDTO>> SearchConnectionBridgeAsync(ConnectionBridgeQueryDTO model, UserContextDTO user)
        {
            if (model.SConnectionId.HasValue == model.SDataType.HasValue)
                throw new DCPBadRequestException("Invalid Source Specificaiton");

            if (model.TConnectionId.HasValue == model.TDataType.HasValue)
                throw new DCPBadRequestException("Invalid Target Specificaiton");

            var query = _dbContext.ConnectionBridge
                .Where(x => x.SCon.SiteId == model.SiteId);

            if (model.SConnectionId.HasValue)
            {
                query = query.Where(x => x.SConId == model.SConnectionId.Value);
            }

            if (model.TConnectionId.HasValue)
            {
                query = query.Where(x => x.TConId == model.TConnectionId.Value);
            }

            if (model.SDataType.HasValue)
            {
                query = query.Where(x => x.SConDataType == (byte)model.SDataType.Value);
            }

            if (model.TDataType.HasValue)
            {
                query = query.Where(x => x.TConDataType == (byte)model.TDataType.Value);
            }

            var result = await query
                .Select(x => new ConnectionBridgeDTO
                {
                    Strategy = x.Strategy,
                    Definition = x.Definition ?? new DefaultStrategyDefintion(),
                    SConId = x.SConId,
                    TConId = x.TConId,
                }
                ).AsNoTracking()
                .ToListAsync();

            return result;
        }
    }
}
