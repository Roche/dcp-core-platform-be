using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.DAL.Extensions;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Legacy.Dashboard;
using OpenDCP.Framework.Dashboard.Messages;
using OpenDCP.Framework.Dashboard.Models;
using OpenDCP.Framework.Dashboard.Requests;
using OpenDCP.Framework.DataTypes.Equipment.Extensions;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.Security.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class DashboardService : BaseService, IDashboardService
    {
        private readonly IMessageBus _messageBus;

        public DashboardService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache, IMessageBus messageBus)
            : base(dbContext, configuration, factory, memoryCache)
        {
            _messageBus = messageBus;
        }

        public async Task<DashboardTabDTO> CreateDashboardTabAsync(DashboardTabConfigurationDTO model, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                .Where(x => x.UnixId == user.UnixId)
                .Select(x => x.CurrentSiteId)
                .FirstOrDefaultAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            if (string.IsNullOrEmpty(model.Name))
            {
                throw new DCPBadRequestException(ErrorMessages.DashboardTabRequiredParameter);
            }

            var entityToAdd = new DashboardTab
            {
                Name = model.Name,
                SiteId = siteId.Value,
                OwnerUserId = user.UserId,
                LastModifiedBy = user.UserId,
                Version = 1
            };

            _dbContext.DashboardTab.Add(entityToAdd);

            await _dbContext.SaveChangesAsync();

            return new DashboardTabDTO
            {
                Id = entityToAdd.Id,
                Name = model.Name,
                SiteId = siteId.Value,
                OwnerUserId = user.UserId,
                LastModifiedBy = user.UserId,
                LastModifiedAt = DateTime.UtcNow,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };
        }

        public async Task<DashboardTabDTO> UpdateDashboardTabAsync(Guid dashboardTabId, DashboardTabConfigurationDTO model, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                .Where(x => x.UnixId == user.UnixId)
                .Select(x => x.CurrentSiteId)
                .FirstOrDefaultAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            var existingEntity = await _dbContext.DashboardTab
                .FirstOrDefaultAsync(x => x.Id == dashboardTabId
                                          && x.SiteId == siteId);

            if (existingEntity == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardTabNotFound);
            }

            existingEntity.Name = model.Name;
            existingEntity.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            return new DashboardTabDTO
            {
                Id = existingEntity.Id,
                Name = model.Name,
                SiteId = existingEntity.SiteId,
                OwnerUserId = existingEntity.OwnerUserId,
                LastModifiedBy = user.UserId,
                LastModifiedAt = DateTime.UtcNow,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };
        }

        public async Task DeleteDashboardTabAsync(Guid dashboardTabId, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                  .Where(x => x.Id == user.UserId)
                  .Select(x => x.CurrentSiteId)
                  .FirstAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            var entityForDelete = await _dbContext.DashboardTab
                .FirstOrDefaultAsync(x => x.Id == dashboardTabId
                                          && x.SiteId == siteId);

            if (entityForDelete == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardTabNotFound);
            }

            //For audit trail purpose 
            entityForDelete.LastModifiedBy = user.UserId;

            var dashboardExports = _dbContext.DashboardExport
                .Where(x => x.DashboardTabId == dashboardTabId);

            await dashboardExports
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.LastModifiedBy, user.UserId));

            var dashboardExportsForDelete = await dashboardExports.ToListAsync();

            await _dbContext.SaveChangesAsync();

            //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            //send request to Rabbit MQ that dashboard tab is deleted
            //Use this requestModel for request with key DashboardTabsDeleteRoutingKey
            var requestModel = new List<DashboardTabDTO>();
            var itemToAdd = new DashboardTabDTO
            {
                SiteId = entityForDelete.SiteId,
                Id = entityForDelete.Id,
                Name = entityForDelete.Name,
                LastModifiedBy = user.UserId,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };
            requestModel.Add(itemToAdd);

            var jMessage = JsonDocument.Parse(JsonSerializer.Serialize(requestModel)).RootElement;

            _messageBus.Publish(
                message: jMessage,
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

            _dbContext.DashboardExport.RemoveRange(dashboardExportsForDelete);
            _dbContext.DashboardTab.Remove(entityForDelete);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<DashboardTabDTO>> SearchDashboardTabsAsync(UserContextDTO user, DashboardTabQueryDTO? model = null)
        {
            //Cardinality Validation
            if (model != null && model.DashboardTabId.Count > 1 && model.SiteId.Count > 1)
            {
                throw new DCPBadRequestException(ErrorMessages.SearchCardinalityProductProblem);
            }

            List<int> sites = new List<int>();

            if (model != null && model.SiteId.Any())
            {
                sites = model.SiteId;
            }
            else
            {
                var currentSiteId = await _dbContext.User
                    .Where(x => x.UnixId == user.UnixId)
                    .Select(x => x.CurrentSiteId)
                    .FirstOrDefaultAsync();

                if (currentSiteId.HasValue) sites.Add(currentSiteId.Value);
            }

            var requiredPermissions = new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            };

            foreach (var siteId in sites)
            {
                HasPermission(requiredPermissions, user, siteId, true);
            }

            var query = _dbContext.DashboardTab.AsNoTracking().AsQueryable();

            if (sites.Any())
            {
                query = query.Where(x => sites.Contains(x.SiteId));
            }

            if (model != null && model.DashboardTabId.Any())
            {
                query = query.Where(x => model.DashboardTabId.Contains(x.Id));
            }
            else if (model == null)
            {
                query = query.Where(x => x.OwnerUserId == user.UserId);
            }

            var dashboardTabs = await query
                .Select(x => new DashboardTabDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    SiteId = x.SiteId,
                    OwnerUserId = x.OwnerUserId,
                    LastModifiedBy = x.LastModifiedBy,
                    LastModifiedAt = x.SysStartTime,
                    IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
                })
                .ToListAsync();

            return dashboardTabs;
        }

        public async Task<Guid> ExportDashboardAsync(Guid dashboardTabId, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                  .Where(x => x.Id == user.UserId)
                  .Select(x => x.CurrentSiteId)
                  .FirstAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            var existingEntity = await _dbContext.DashboardTab
                .FirstOrDefaultAsync(x => x.Id == dashboardTabId
                                          && x.SiteId == siteId);

            if (existingEntity == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardTabNotFound);
            }

            var dashboardExportGuid = Guid.NewGuid();


            var dashboardExportToAdd = new DashboardExport
            {
                DashboardExportGuid = dashboardExportGuid,
                UserId = user.UserId!.Value,
                SiteId = siteId.Value,
                DashboardTabId = existingEntity.Id,
                DashboardTab = existingEntity,
                DashboardTabVersion = existingEntity.Version,
                ExportDate = DateTime.UtcNow,
                OwnerUserId = user.UserId,
                LastModifiedBy = user.UserId
            };

            _dbContext.DashboardExport.Add(dashboardExportToAdd);

            await _dbContext.SaveChangesAsync();

            return dashboardExportGuid;
        }

        public async Task DeleteDashboardExportAsync(Guid dashboardExportGuid, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                  .Where(x => x.Id == user.UserId)
                  .Select(x => x.CurrentSiteId)
                  .FirstAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            var dashboardExportForDelete = await _dbContext.DashboardExport
                .FirstOrDefaultAsync(x => x.DashboardExportGuid == dashboardExportGuid);

            if (dashboardExportForDelete == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardExportNotFound);
            }

            dashboardExportForDelete.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            //Add delay because of EF Core behavior not storing the update if records are first updated and then deleted
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.DashboardExport.Remove(dashboardExportForDelete);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<Guid> ImportDashboardAsync(DashboardImportDTO model, UserContextDTO user)
        {
            var siteId = await _dbContext.User
                 .Where(x => x.Id == user.UserId)
                 .Select(x => x.CurrentSiteId)
                 .FirstAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId.Value, true);

            if (string.IsNullOrEmpty(model.Name))
            {
                throw new DCPBadRequestException(ErrorMessages.DashboardTabRequiredParameter);
            }

            var dialect = _dbContext.GetDialect();
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var dashboardExport = await dialect.GetDashboardExportByGuidAsync(_dbContext, model.DashboardImportGUID, token);

            if (dashboardExport == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardExportNotFound);
            }

            var dashboardTabForCreate = new DashboardTab
            {
                Name = model.Name,
                SiteId = siteId.Value,
                OwnerUserId = user.UserId,
                LastModifiedBy = user.UserId,
                Version = 1
            };

            _dbContext.DashboardTab.Add(dashboardTabForCreate);

            await _dbContext.SaveChangesAsync();

            var requestModel = new ImportDashboardMessage
            {
                SiteId = siteId.Value,
                ExportDate = dashboardExport.ExportDate,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                UserId = user.UserId!.Value,
                SourceDashboardTabId = dashboardExport.DashboardTabId,
                TargetDashboardTabId = dashboardTabForCreate.Id
            };

            var jMessage = JsonDocument.Parse(JsonSerializer.Serialize(requestModel)).RootElement;

            _messageBus.Publish(
                message: jMessage,
                exchangerName: MessageExchangers.EventBus,
                keys: new List<string> { RoutingKeys.DashboardImport }
                );

            SendMessageToLegacyExchanger(requestModel);

            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            return dashboardTabForCreate.Id;
        }

        private void SendMessageToLegacyExchanger(ImportDashboardMessage requestModel)
        {
            var requestModelLegacy = new DashboardImportRequest
            {
                CurrentUserId = requestModel.UserId,
                ExportDate = requestModel.ExportDate.ToIso8601String(),
                NewDashboardTabId = requestModel.TargetDashboardTabId,
                OldDashboardTabId = requestModel.SourceDashboardTabId,
                SiteConfiguration = new Framework.Configuration.Model.Entities.Site.LegacySiteConfigurationDTO_v2
                {
                    ID = requestModel.SiteId,
                },
                IsGxp = requestModel.IsGxp,
            };

            var jMessageLegacy = JsonDocument.Parse(JsonSerializer.Serialize(requestModelLegacy)).RootElement;

            _messageBus.Publish(
                message: jMessageLegacy,
                exchangerName: MessageExchangers.LegacyEventBus,
                keys: new List<string> { RoutingKeys.DashboardImport }
                );
        }

        public async Task UpdateDashboardTabLayoutAsync(int siteId, DashboardTabLayoutConfigurationDTO model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId, true);

            bool isAdmin = user.Permissions.Any(x => x.Permission == Permissions.ModuleIndependent.ADMIN && x.SiteId == siteId);
            var existingEntity = await _dbContext.DashboardTab
                .FirstOrDefaultAsync(x => x.Id == model.DashboardTabId && x.SiteId == siteId);

            if (existingEntity == null)
            {
                throw new DCPNotFoundException(ErrorMessages.DashboardTabNotFound);
            }

            bool isOwner = existingEntity.OwnerUserId == user.UserId;

            if (!isOwner && !isAdmin)
            {
                throw new DCPBadRequestException(ErrorMessages.CannotChangeDashboardTabLayout);
            }

            var requestModel = new DashboardTabLayoutDTO
            {
                DashboardTabId = model.DashboardTabId,
                DashboardTabLayoutDetails = model.DashboardTabLayoutDetails,
                SiteId = siteId,
                UserId = user.UserId!.Value,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };

            var jMessage = JsonDocument.Parse(JsonSerializer.Serialize(requestModel)).RootElement;

            _messageBus.Publish(
                message: jMessage,
                exchangerName: MessageExchangers.EventBus,
                keys: new List<string> { RoutingKeys.UpdateDashboardLayouts }
                );

            var dashboardLayoutDetails = model.DashboardTabLayoutDetails.Select(d => new DashboardLayoutDetailsDTO
            {
                ID = d.Id,
                GridLayout = JObject.FromObject(d.GridLayout),
            }).ToList();

            var requestModelLegacy = new DashboardLayoutDTO
            {
                DashboardTabId = requestModel.DashboardTabId,
                DashboardLayoutDetails = dashboardLayoutDetails,
                CurrentUserId = requestModel.UserId,
                IsGxp = requestModel.IsGxp,
            };

            var jMessageLegacy = JsonDocument.Parse(JsonSerializer.Serialize(requestModelLegacy)).RootElement;

            _messageBus.Publish(
                message: jMessageLegacy,
                exchangerName: MessageExchangers.LegacyEventBus,
                keys: new List<string> { RoutingKeys.UpdateDashboardLayouts }
                );
        }

        public async Task<List<DashboardExportDTO>> SearchDashboardExportAsync(UserContextDTO user, DashboardExportQueryDTO? model = null)
        {
            //Cardinality Validation
            if (model != null && model.DashboardExportId.Count > 1 && model.SiteId.Count > 1)
            {
                throw new DCPBadRequestException(ErrorMessages.SearchCardinalityProductProblem);
            }

            List<int> sites = new List<int>();

            if (model != null && model.SiteId.Any())
            {
                sites = model.SiteId;
            }
            else
            {
                var currentSiteId = await _dbContext.User
                    .Where(x => x.UnixId == user.UnixId)
                    .Select(x => x.CurrentSiteId)
                    .FirstOrDefaultAsync();

                if (currentSiteId.HasValue) sites.Add(currentSiteId.Value);
            }

            var requiredPermissions = new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            };

            foreach (var siteId in sites)
            {
                HasPermission(requiredPermissions, user, siteId, true);
            }

            var query = _dbContext.DashboardExport
                .AsNoTracking()
                .AsQueryable();

            if (sites.Any())
            {
                query = query.Where(x => sites.Contains(x.SiteId));
            }

            if (model != null && model.DashboardExportId.Any())
            {
                query = query.Where(x => model.DashboardExportId.Contains(x.DashboardExportGuid));
            }
            else if (model == null)
            {
                query = query.Where(x => x.OwnerUserId == user.UserId);
            }

            var dashboardTabs = await query
                .Select(x => new DashboardExportDTO
                {
                    Id = x.DashboardExportGuid,
                    TabName = x.DashboardTab.Name,
                    TabId = x.DashboardTabId,
                    ExportDate = x.ExportDate,
                    TabVersion = (short)x.DashboardTabVersion,
                    SiteId = x.SiteId,
                    OwnerUserId = x.OwnerUserId,
                    LastModifiedBy = x.LastModifiedBy,
                    LastModifiedAt = x.SysStartTime,
                })
                .ToListAsync();

            return dashboardTabs;
        }
    }
}
