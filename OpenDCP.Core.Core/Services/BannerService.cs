using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Banner;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Constants;
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
    public class BannerService : BaseService, IBannerService
    {
        public BannerService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache) : base(dbContext, configuration, factory, memoryCache)
        {
            
        }

        public async Task<BannerConfigurationDTO> CreateBannerAsync(BannerConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPUnauthorizedException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var banner = new UserInfoBanner
            {
                SiteId = model.SiteId,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Category = model.Category,
                Description = model.Description,
                IsActive = model.IsActive,
                ModuleName = model.ModuleName,
                Name = model.Name,
                LastModifiedBy = user.UserId,
                OwnerUserId = user.UserId,
            };

            await _dbContext.UserInfoBanner.AddAsync(banner);

            await _dbContext.SaveChangesAsync();

            model.Id = banner.Id;

            return model;
        }

        public async Task DeleteBannerAsync(int bannerId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPUnauthorizedException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var banner = await _dbContext.UserInfoBanner
                .FirstOrDefaultAsync(x => x.Id == bannerId);

            if(banner == null)
            {
                throw new DCPNotFoundException(ErrorMessages.UnkownBanner);
            }

            banner.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            //Add delay to have audit trail correct
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.UserInfoBanner.Remove(banner);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<BannerDTO>> GetAllBannersAsync(UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPUnauthorizedException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var banners = await _dbContext.UserInfoBanner
                .Select(x => new 
                {
                    Id = x.Id,
                    SiteId = x.SiteId,
                    Name = x.Name,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    IsActive = x.IsActive,
                    ModuleName = x.ModuleName,
                    Category = x.Category,
                    Description = x.Description,
                    LastModifiedBy = x.LastModifiedBy,
                    CreatedBy = x.OwnerUserId
                })
                .AsNoTracking()
                .ToListAsync();

            var usersIds = banners
                .SelectMany(x => new[] { x.LastModifiedBy, x.CreatedBy })
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var users = await _dbContext.User
                .Where(x => usersIds.Contains(x.Id))
                .Select(u => new
                {
                    u.Id,
                    u.DisplayName
                })
                .AsNoTracking()
                .ToListAsync();

            var usersDict = users.ToDictionary(u => u.Id, u => u.DisplayName);

            return banners
                .Select(b => new BannerDTO
                {
                    Id = b.Id,
                    SiteId = b.SiteId,
                    Name = b.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    IsActive = b.IsActive,
                    ModuleName = b.ModuleName,
                    Category = b.Category,
                    Description = b.Description,

                    LastModifiedBy =
                        b.LastModifiedBy.HasValue &&
                        usersDict.TryGetValue(b.LastModifiedBy.Value, out var mname)
                            ? mname
                            : "System",

                    CreatedBy =
                        b.CreatedBy.HasValue &&
                        usersDict.TryGetValue(b.CreatedBy.Value, out var cname)
                            ? cname
                            : "System"
                })
                .ToList();
        }

        public async Task SetBannerStatusAsync(int bannerId, bool isEnabled, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPUnauthorizedException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var banner = await _dbContext.UserInfoBanner
                .FirstOrDefaultAsync(x => x.Id == bannerId);

            if (banner == null)
            {
                throw new DCPNotFoundException(ErrorMessages.UnkownBanner);
            }

            banner.IsActive = isEnabled;
            banner.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<BannerConfigurationDTO> UpdateBannerAsync(BannerConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPUnauthorizedException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            var banner = await _dbContext.UserInfoBanner
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (banner == null)
            {
                throw new DCPNotFoundException(ErrorMessages.UnkownBanner);
            }

            banner.IsActive = model.IsActive;
            banner.LastModifiedBy = user.UserId;
            banner.SiteId = model.SiteId;
            banner.Name = model.Name;
            banner.Description = model.Description;
            banner.Category = model.Category;
            banner.ModuleName = model.ModuleName;
            banner.StartTime = model.StartTime;
            banner.EndTime = model.EndTime;

            await _dbContext.SaveChangesAsync();

            return model;
        }

        public async Task<List<BannerDisplayDTO>> GetActiveBannersAsync()
        {
            var banners = await _dbContext.UserInfoBanner
                .Where(x => x.IsActive
                            && x.EndTime >= DateTime.UtcNow
                            && x.StartTime <= DateTime.UtcNow
                 )
                .Select(x => new BannerDisplayDTO
                {
                    Id = x.Id,
                    SiteId = x.SiteId,
                    ModuleName = x.ModuleName,
                    Category = x.Category,
                    Description = x.Description,
                })
                .AsNoTracking()
                .ToListAsync();

            return banners;
        }
    }
}
