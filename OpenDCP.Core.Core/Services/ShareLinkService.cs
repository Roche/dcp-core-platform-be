using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.DAL.Extensions;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class ShareLinkService : BaseService, IShareLinkService
    {
        public ShareLinkService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache) : base(dbContext, configuration, factory, memoryCache)
        {
        }

        public async Task<ShareLinkAliasDTO> CreateShareLinkAliasAsync(ShareLinkAliasConfigurationDTO model, UserContextDTO user)
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
                throw new DCPBadRequestException(ErrorMessages.ShareLinkAliasNameRequired);
            }

            if (await _dbContext.ShareLinkAlias.AnyAsync(x => x.Name == model.Name && x.ShareLink!.SiteId == siteId))
            {
                throw new DCPBadRequestException(ErrorMessages.AliasNameNeedUniqueForGivenSite);
            }

            var sharedLinkDetails = await _dbContext.ShareLink
                .Where(x => !x.IsDeleted
                            && x.LinkGuid == model.ShareLinkGuid
                            && x.SiteId == siteId)
                  .Select(x => new
                  {
                      x.Id
                  })
                  .FirstOrDefaultAsync();

            if (sharedLinkDetails == null)
            {
                throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);

            }

            var entityForAdd = new ShareLinkAlias
            {
                LastModifiedBy = user.UserId,
                OwnerUserId = user.UserId,
                Name = model.Name,
                ShareLinkId = sharedLinkDetails.Id,
            };

            _dbContext.ShareLinkAlias.Add(entityForAdd);

            await _dbContext.SaveChangesAsync();

            return new ShareLinkAliasDTO
            {
                Id = entityForAdd.Id,
                Name = model.Name,
                ShareLinkGuid = model.ShareLinkGuid,
                OwnerUserId = user.UserId!.Value,
                LastModifiedBy = user.UserId!.Value,
                LastModifiedAt = DateTime.UtcNow,
            };
        }

        public async Task<List<ShareLinkDTO>> CreateSystemShareLinkAsync(List<ShareLinkConfigurationDTO> model, UserContextDTO user)
        {
            var systemGeneratedSharedLinksForAdd = new List<ShareLinkSystemGenerated>();

            foreach (var item in model)
            {
                var itemToAdd = new ShareLinkSystemGenerated
                {
                    DateCreated = DateTime.UtcNow,
                    IsDeleted = false,
                    JLink = item.Link,
                    LinkGuid = item.LinkGUID,
                    SiteId = item.SiteId
                };
                systemGeneratedSharedLinksForAdd.Add(itemToAdd);
            }

            await _dbContext.ShareLinkSystemGenerated.AddRangeAsync(systemGeneratedSharedLinksForAdd);

            await _dbContext.SaveChangesAsync();

            var result = model.Select(x => new ShareLinkDTO
            {
                Guid = x.LinkGUID,
                Link = x.Link,
                Title = x.Title,
            }).ToList();

            return result;
        }

        public async Task<ShareLinkDTO> CreateUserShareLinkAsync(ShareLinkConfigurationDTO model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, model.SiteId!.Value, true);

            var shareLink = new ShareLink
            {
                Title = model.Title,
                IsDeleted = false,
                JLink = model.Link,
                LinkGuid = Guid.NewGuid(),
                SiteId = model.SiteId,
                LastModifiedBy = user.UserId,
                OwnerUserId = user.UserId,
            };

            await _dbContext.ShareLink.AddAsync(shareLink);

            await _dbContext.SaveChangesAsync();

            var result = new ShareLinkDTO
            {
                Title = shareLink.Title,
                Guid = shareLink.LinkGuid,
                Link = shareLink.JLink,
                LastModifiedBy = shareLink.LastModifiedBy!.Value,
                LastModifiedAt = shareLink.SysStartTime
            };

            return result;
        }

        public async Task DeleteShareLinkAliasAsync(int id, Guid guid, UserContextDTO user)
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


            var sharedLinkDetails = await _dbContext.ShareLink
                .Where(x => !x.IsDeleted
                            && x.LinkGuid == guid
                            && x.SiteId == siteId)
                .Select(x => new
                {
                    x.Id
                })
                .FirstOrDefaultAsync();

            if (sharedLinkDetails == null)
            {
                throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);
            }

            var entityForDelete = await _dbContext.ShareLinkAlias
                .FirstOrDefaultAsync(x => x.Id == id
                                          && x.ShareLinkId == sharedLinkDetails.Id);

            if (entityForDelete == null)
            {
                throw new DCPNotFoundException(ErrorMessages.ShareLinkAliasNotFound);
            }

            entityForDelete.LastModifiedBy = user.UserId;


            //Adding historical record with what is the reason for delete the given site
            //In the audit trail this record will appear
            await _dbContext.SaveChangesAsync();

            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.ShareLinkAlias.Remove(entityForDelete);

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteShareLinkAsync(Guid guid, UserContextDTO user)
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

            var existingEntity = await _dbContext.ShareLink
                .Include(x => x.ShareLinkAliases)
                .FirstOrDefaultAsync(x => x.LinkGuid == guid
                                          && !x.IsDeleted
                                          && x.SiteId == siteId);

            if (existingEntity == null)
            {
                throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);
            }

            foreach (var alias in existingEntity.ShareLinkAliases)
            {
                alias.LastModifiedBy = user.UserId;
            }

            existingEntity.IsDeleted = true;
            existingEntity.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.ShareLinkAlias.RemoveRange(existingEntity.ShareLinkAliases);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<LegacyShareLinkListItemDTO>> GetLegacyUserShareLinksAsync(int siteId, UserContextDTO user)
        {
            var userCurrentSiteId = await _dbContext.User
                .Where(x => x.UnixId == user.UnixId)
                .Select(x => x.CurrentSiteId)
                .FirstOrDefaultAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, userCurrentSiteId.Value, true);

            var existingLinks = await _dbContext.ShareLink
                .Where(x => x.SiteId == userCurrentSiteId && !x.IsDeleted && x.OwnerUserId == user.UserId)
                .ToListAsync();

            var shareLinksIds = existingLinks.Select(x => x.Id);

            var shareLinkAlias = await _dbContext.ShareLinkAlias
                .Where(x => shareLinksIds.Contains(x.ShareLinkId))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ShareLink!.LinkGuid,
                    x.ShareLinkId
                })
                .ToListAsync();

            var result = new List<LegacyShareLinkListItemDTO>();

            foreach (var item in existingLinks)
            {
                var itemToAdd = new LegacyShareLinkListItemDTO
                {
                    UserIDCreated = item.OwnerUserId!.Value,
                    DateCreated = item.SysStartTime,
                    Guid = item.LinkGuid,
                    Link = item.JLink,
                    Title = item.Title,
                    ShareLinkAliasDetails = new List<LegacySharedLinkAliasDTO>()
                };

                var shareLinkAliasDetails = shareLinkAlias.Where(x => x.ShareLinkId == item.Id);

                foreach (var shareLinkAliasDetail in shareLinkAliasDetails)
                {
                    itemToAdd.ShareLinkAliasDetails.Add(new LegacySharedLinkAliasDTO
                    {
                        ID = shareLinkAliasDetail.Id,
                        Name = shareLinkAliasDetail.Name,
                        SharedLinkGuid = shareLinkAliasDetail.LinkGuid
                    });
                }

                result.Add(itemToAdd);
            }

            result = result.OrderByDescending(x => x.DateCreated).ToList();

            return result;
        }

        public async Task<List<ShareLinkListItemDTO>> GetUserShareLinksAsync(int siteId, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId, true);

            var dialect = _dbContext.GetDialect();
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var result = await dialect.GetShareLinksAsync(_dbContext, siteId, user.UserId!.Value, token);

            var shareLinksIds = result.Select(x => x.Guid).ToList();

            var shareLinkAlias = await _dbContext.ShareLinkAlias
                .Where(x => shareLinksIds.Contains(x.ShareLink.LinkGuid))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ShareLink!.LinkGuid,
                })
                .ToListAsync();

            foreach (var item in result)
            {
                var shareLinkAliasDetails = shareLinkAlias.Where(x => x.LinkGuid == item.Guid);

                foreach (var shareLinkAliasDetail in shareLinkAliasDetails)
                {
                    item.Aliases.Add(new ShareLinkAliasDisplayDTO
                    {
                        Id = shareLinkAliasDetail.Id,
                        Alias = shareLinkAliasDetail.Name,
                    });
                }

            }

            return result;
        }

        public async Task<ShareLinkListItemDTO> SearchAliasAsync(string alias, UserContextDTO user, int? siteId)
        {
            var aliasDetails = await _dbContext.ShareLinkAlias
                .Where(x => x.Name == alias)
                .Select(x => new
                {
                    Guid = x.ShareLink.LinkGuid,
                    Link = x.ShareLink.JLink,
                    Title = x.ShareLink.Title,
                    SiteId = x.ShareLink.SiteId
                })
                .FirstOrDefaultAsync() ?? throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);

            HasPermission(
            [
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            ], user, aliasDetails.SiteId.Value, true);

            return new ShareLinkListItemDTO
            {
                Guid = aliasDetails.Guid,
                Title = aliasDetails.Title,
                Page = aliasDetails.Link["sharedPage"]?.ToString()
            };
        }

        public async Task<List<ShareLinkAliasDTO>> SearchLinkAliasAsync(List<int?>? siteIds, List<int?>? ownerIds, UserContextDTO user)
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

            var query = _dbContext.ShareLinkAlias.AsQueryable();

            if (siteIds != null && siteIds.Any())
            {
                query = query.Where(x => siteIds.Contains(x.ShareLink!.SiteId));
            }

            if (ownerIds != null && ownerIds.Any())
            {
                query = query.Where(x => ownerIds.Contains(x.OwnerUserId));
            }

            var shareLinkAlias = await query
                .Select(x => new ShareLinkAliasDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShareLinkGuid = x.ShareLink!.LinkGuid,
                    OwnerUserId = x.OwnerUserId!.Value,
                    LastModifiedBy = x.LastModifiedBy!.Value,
                    LastModifiedAt = x.SysStartTime
                })
                .ToListAsync();

            return shareLinkAlias;
        }

        public async Task<List<ShareLinkDTO>> SearchShareLinkAsync(ShareLinkQueryDTO model, UserContextDTO user)
        {
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

            var result = new List<ShareLinkDTO>();

            result.AddRange(await _dbContext.ShareLink
                .Where(x => model.Guid.Contains(x.LinkGuid))
                .Select(x => new ShareLinkDTO
                {
                    Guid = x.LinkGuid,
                    Link = x.JLink,
                    Title = x.Title,
                    LastModifiedBy = x.LastModifiedBy!.Value,
                    LastModifiedAt = x.SysStartTime
                })
                .ToListAsync());


            result.AddRange(await _dbContext.ShareLinkSystemGenerated
                .Where(x => model.Guid.Contains(x.LinkGuid))
                .Select(x => new ShareLinkDTO
                {
                    Guid = x.LinkGuid,
                    Link = x.JLink,
                    Title = string.Empty
                })
                .ToListAsync());

            result.AddRange(await _dbContext.ShareLinkAlias
                .Where(x => model.Alias.Contains(x.Name))
                .Select(x => new ShareLinkDTO
                {
                    Guid = x.ShareLink!.LinkGuid,
                    Title = x.ShareLink.Title,
                    Link = x.ShareLink.JLink,
                    LastModifiedBy = x.ShareLink.LastModifiedBy!.Value,
                    LastModifiedAt = x.ShareLink.SysStartTime,
                    Aliases = x.ShareLink.ShareLinkAliases.Select(a =>
                    new ShareLinkAliasDisplayDTO
                    {
                        Id = a.Id,
                        Alias = a.Name,
                    }).ToList()
                })
                .ToListAsync());

            if (result.Count != model.Alias.Count + model.Guid.Count)
            {
                throw new DCPBadRequestException(ErrorMessages.SomeElementsCouldNotBeFound);
            }

            return result;
        }

        public async Task<ShareLinkAliasDTO> UpdateShareLinkAliasAsync(ShareLinkAliasConfigurationDTO model, int id, UserContextDTO user)
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
                throw new DCPBadRequestException(ErrorMessages.ShareLinkAliasNameRequired);
            }

            var sharedLinkDetails = await _dbContext.ShareLink
                .Where(x => !x.IsDeleted
                            && x.LinkGuid == model.ShareLinkGuid
                            && x.SiteId == siteId)
               .Select(x => new
               {
                   x.Id
               }).FirstOrDefaultAsync();
            if (sharedLinkDetails == null)
            {
                throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);

            }

            var entityForUpdate = await _dbContext.ShareLinkAlias.FirstOrDefaultAsync(x => x.Id == id && x.ShareLink!.SiteId == siteId);

            if (entityForUpdate == null)
            {
                throw new DCPNotFoundException(ErrorMessages.ShareLinkAliasNotFound);
            }

            entityForUpdate.Name = model.Name;
            entityForUpdate.ShareLinkId = sharedLinkDetails.Id;
            entityForUpdate.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            return new ShareLinkAliasDTO
            {
                Id = id,
                Name = model.Name,
                ShareLinkGuid = model.ShareLinkGuid,
                LastModifiedBy = user.UserId!.Value,
                LastModifiedAt = DateTime.UtcNow,
            };
        }

        public async Task<ShareLinkDTO> UpdateUserShareLinkAsync(ShareLinkConfigurationUpdateDTO model, Guid guid, UserContextDTO user)
        {
            if (string.IsNullOrEmpty(model.Title))
            {
                throw new DCPBadRequestException(ErrorMessages.GlobalFilterLinkRequiredParameters);

            }

            var existingEntity = await _dbContext.ShareLink
                .FirstOrDefaultAsync(x => !x.IsDeleted
                                          && x.LinkGuid == model.LinkGUID);

            if (existingEntity == null)
            {
                throw new DCPNotFoundException(ErrorMessages.CannotFindGlobalFilterLink);
            }

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, existingEntity.SiteId!.Value, true);

            existingEntity.Title = model.Title;
            existingEntity.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            return new ShareLinkDTO
            {
                Guid = model.LinkGUID,
                Link = existingEntity.JLink,
                Title = model.Title,
                LastModifiedBy = user.UserId!.Value,
                LastModifiedAt = existingEntity.SysStartTime,
            };
        }
    }
}
