using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.UserClient;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class UserClientService : BaseService, IUserClientService
    {
        public UserClientService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache) : base(dbContext, configuration, factory, memoryCache)
        {
        }

        public async Task<UserClientConfigurationDTO> CreateUserClientAsync(UserClientUserConfigurationDTO model, UserContextDTO user)
        {
            if (user.UserId is null)
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);

            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                                      || p.Permission == Permissions.Global.APIUSER))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            if (await _dbContext.UserClient.AnyAsync(x => x.Name == model.Name && x.OwnerUserId == user.UserId))
            {
                throw new DCPBadRequestException("User Client with same name exists");
            }

            var key = GetEncryptionKey();
            var clientSecret = EncryptionHelper.GenerateKey();

            var client = new UserClient
            {
                Name = model.Name,
                Description = model.Description,
                ClientId = EncryptionHelper.GenerateKey(),
                ClientSecret = EncryptionHelper.Encrypt(clientSecret, key),
                Permissions = model.Permissions,
                Status = model.Status,
                OwnerUserId = user.UserId.Value,
                LastModifiedBy = user.UserId.Value
            };

            await _dbContext.UserClient.AddAsync(client);

            await _dbContext.SaveChangesAsync();

            var userDetails = await _dbContext.User
                .Select(x => x.DisplayName)
                .AsNoTracking()
                .FirstAsync();

            var result = new UserClientConfigurationDTO
            {
                Description = model.Description,
                ClientId = client.ClientId,
                //Only returned once - on create
                ClientSecret = clientSecret,
                Id = client.Id,
                Name = model.Name,
                Permissions = model.Permissions,
                Status = model.Status,
                OwnerUserId = user.UserId.Value,
                LastModifiedBy = userDetails,
                OwnedBy = userDetails,
                LastModifiedAt = client.SysStartTime
            };

            return result;
        }

        public async Task DeleteUserClientAsync(int userClientId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                                       || p.Permission == Permissions.Global.APIUSER))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var client = await _dbContext.UserClient.FirstOrDefaultAsync(x => x.Id == userClientId);

            if (client == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUserClient);
            }

            if (client.OwnerUserId != user.UserId
                && !user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            client.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();
            await Task.Delay(EntityFrameworkConfiguration.SaveChangesDelayInMilliseconds);

            _dbContext.UserClient.Remove(client);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<UserClientConfigurationDTO>> GetAllUserClientsAsync(UserContextDTO user)
        {
            if(!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN 
                                        || p.Permission == Permissions.Global.APIUSER))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var query = _dbContext.UserClient.AsQueryable();

            //Global Admin can see all - API user only his own client
            if(!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                query = query.Where(x => x.OwnerUserId == user.UserId);
            }

            var result = await query
                .Select(x => new UserClientConfigurationDTO
                {
                            Id = x.Id,
                            OwnerUserId = x.OwnerUserId,
                            Description = x.Description,
                            ClientId = x.ClientId,
                            //Client secrets should never be returned
                            ClientSecret = string.Empty,
                            Name = x.Name,
                            OwnedBy = x.Owner!.DisplayName,
                            Status = x.Status,
                            Permissions = x.Permissions,
                            LastModifiedAt = x.SysStartTime
                        })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<UserClientConfigurationDTO> GetUserClientAsync(int userClientId, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                                        || p.Permission == Permissions.Global.APIUSER))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var query = _dbContext.UserClient.AsQueryable();

            var result = await query
                .Where(x => x.Id == userClientId)
                .Select(x => new UserClientConfigurationDTO
                {
                    Id = x.Id,
                    OwnerUserId = x.OwnerUserId,
                    Description = x.Description,
                    ClientId = x.ClientId,
                    //Client secrets should never be returned
                    ClientSecret = string.Empty,
                    Name = x.Name,
                    OwnedBy = x.Owner!.DisplayName,
                    Status = x.Status,
                    Permissions = x.Permissions,
                    LastModifiedAt = x.SysStartTime
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if(result == null)
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidClient);
            }

            return result;
        }

        public async Task SetUserClientStatusAsync(int userClientId, UserClientStatus status, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                                       || p.Permission == Permissions.Global.APIUSER)
                || !user.UserId.HasValue)
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var client = await _dbContext.UserClient.FirstOrDefaultAsync(x => x.Id == userClientId);

            if(client == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUserClient);
            }

            if(client.OwnerUserId != user.UserId 
                && !user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            client.LastModifiedBy = user.UserId.Value;
            client.Status = status;

            await _dbContext.SaveChangesAsync();

        }

        public async Task<UserClientConfigurationDTO> UpdateUserClientAsync(UserClientUserConfigurationDTO model, UserContextDTO user)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN
                                      || p.Permission == Permissions.Global.APIUSER))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            var client = await _dbContext.UserClient
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (client == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUserClient);
            }

            if (client.OwnerUserId != user.UserId
                && !user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (await _dbContext.UserClient.AnyAsync(x => x.Name == model.Name 
                                                && x.Id != model.Id && x.OwnerUserId == client.OwnerUserId))
            {
                throw new DCPBadRequestException("User Client with same name exists");
            }

            client.Name = model.Name;
            client.Description = model.Description;
            client.Permissions = model.Permissions;
            client.Status = model.Status;
            client.LastModifiedBy = user.UserId;

            await _dbContext.SaveChangesAsync();

            var result = new UserClientConfigurationDTO
            {
                Id = model.Id,
                Description = model.Description,
                ClientId = client.ClientId,
                //Never return client secret to FE on update
                ClientSecret = string.Empty, 
                Name = model.Name,
                Permissions = model.Permissions,
                Status = model.Status,
                OwnerUserId = client.OwnerUserId,
                OwnedBy = client.Owner!.DisplayName,
                LastModifiedAt = client.SysStartTime
            };

            return result;
        }
    }
}
