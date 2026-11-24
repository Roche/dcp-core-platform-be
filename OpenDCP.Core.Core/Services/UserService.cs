using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Messages;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Core.Models.Models.User;
using OpenDCP.Framework.AuditTrail.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.MessageDelivery;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.Security.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{

    public class UserService : BaseService, IUserService
    {
        private readonly IMessageBus _messageBus;
        public UserService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache, IMessageBus messageBus) : base(dbContext, configuration, factory, memoryCache)
        {
            _messageBus = messageBus;
        }

        public async Task<List<UserListItemDTO>> GetAllUsersPerSiteAsync(int siteId, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, siteId, true);

            var users = await _dbContext.User
                .Where(x => x.Permissions.Any(p => p.SiteId == siteId))
                .Select(x => new UserListItemDTO
                {
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Id = x.Id,
                    UnixId = x.UnixId,
                })
                .AsNoTracking()
                .ToListAsync();

            return users;
        }

        public async Task<UserDTO> GetUserInfoAsync(UserContextDTO user)
        {
            var userDetails = await _dbContext.User
                .Where(x => x.UnixId == user.UnixId)
                .Select(x => new
                {
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Id = x.Id,
                    UnixId = x.UnixId,
                    Settings = new UserSettingsDTO
                    {
                        DateFormat = x.DateFormat,
                        TimeFormat = x.TimeFormat
                    },
                    Permissions = x.Permissions,
                    LastSessionSiteId = x.CurrentSiteId
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (userDetails == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);
            }

            var userSiteIds = userDetails.Permissions
                .Where(x => (x.Permission == Permissions.ModuleIndependent.VIEWER
                            || x.Permission == Permissions.ModuleIndependent.ADMIN)
                            && x.SiteId > 0
                            )
                .Select(x => x.SiteId)
                .Distinct()
                .ToList();

            var userSites = await _dbContext.Site
                .Where(x => userSiteIds.Contains(x.Id))
                .Select(x => new UserSiteDTO
                {
                    Id = x.Id,
                    IsEnabled = x.IsEnable,
                    Name = x.Name,
                    TimeZone = x.TimeZone
                })
                .AsNoTracking()
                .ToListAsync();

            var userInfo = new UserDTO
            {
                DisplayName = userDetails.DisplayName,
                Email = userDetails.Email,
                Id = userDetails.Id,
                UnixId = userDetails.UnixId,
                Settings = userDetails.Settings,
                Sites = userSites,
                LastSessionSiteId = userDetails.LastSessionSiteId ?? userSiteIds.FirstOrDefault(),
            };


            return userInfo;
        }

        public async Task<List<UserListItemDTO>> QueryUsersAsync(UserQueryDTO model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER,
                Permissions.Global.ADMIN
            }, user, 0, true);

            var query = _dbContext.User.AsQueryable();

            if ((new[] { model.UserId.Count > 0, model.SiteId.Count > 0, model.Email.Count > 0 }).Count(x => x) > 1)
            {
                throw new DCPBadRequestException(ErrorMessages.UnallowedSearchParameterCardinality);
            }

            if (model.UserId.Count > 0)
            {
                query = query.Where(x => model.UserId.Contains(x.Id));
            }

            if (model.Email.Count > 0)
            {
                query = query.Where(x => model.Email.Contains(x.Email));
            }

            if (model.UnixId.Count > 0)
            {
                query = query.Where(x => model.UnixId.Contains(x.UnixId));
            }

            if (model.Module.Count > 0)
            {
                query = query.Where(x => model.Module.Any(
                    m => x.Permissions.Any(p => p.Module == m)));
            }

            if (model.SiteId.Count > 0 && model.Permission.Count > 0)
            {
                query = query.Where(x => x.Permissions.Any(p =>
                                model.SiteId.Contains(p.SiteId) &&
                                model.Permission.Select(sp => sp.ToLower()).Contains(p.Permission.ToLower())));
            }
            else
            {
                if (model.SiteId.Count > 0)
                {
                    query = query.Where(x => x.Permissions.Any(p => model.SiteId.Contains(p.SiteId)));
                }

                if (model.Permission.Count > 0)
                {
                    query = query.Where(x => x.Permissions.Any(p => model.Permission.Select(sp => sp.ToLower()).Contains(p.Permission.ToLower())));
                }
            }

            var users = await query
                .OrderBy(x => x.UnixId)
                .Select(x => new UserListItemDTO
                {
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Id = x.Id,
                    UnixId = x.UnixId,
                })
                .AsNoTracking()
                .ToListAsync();

            return users;
        }

        public async Task SendUserFeedbackAsync(UserFeedbackDTO model, UserContextDTO user)
        {
            var userDetails = await _dbContext.User.FirstOrDefaultAsync(x => x.UnixId == user.UnixId);

            if (userDetails == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);
            }

            var currentUserCurrentSite = await _dbContext.Site
                .Where(x => x.Id == userDetails.CurrentSiteId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, userDetails.CurrentSiteId!.Value, true);

            var message = new UserMessageDTO
            {
                Format = MessageDeliveryMessageFormat.HTML,
                MessageTemplate = "DefaultTemplate.html",
                IsGxP = _configuration.GetValue("Host:IsModuleValidated", false),
                MessageChannels = new List<MessageDeliveryChannelType> { MessageDeliveryChannelType.Mail },
                UserIds = new List<int> { (int)SystemActions.System },
                //Prevents HTML injection
                Message = @$"New feedback sent from a user <strong>{WebUtility.HtmlEncode(userDetails.UnixId)}</strong>

                User Name : <strong>{WebUtility.HtmlEncode($"{userDetails.FirstName} {userDetails.LastName}")}</strong>

                User Roles : <strong>{WebUtility.HtmlEncode(string.Join(", ", userDetails.Permissions.Select(p => p.ToDisplayString())))}</strong>

                Feedback related to: <strong>{WebUtility.HtmlEncode(model.SystemModule)}</strong> for site: <strong>{WebUtility.HtmlEncode(currentUserCurrentSite)}</strong>

                User Feedback: <strong>{WebUtility.HtmlEncode(model.Body)}</strong>"
                .Replace(Environment.NewLine + Environment.NewLine, "<br /><br />")
                .Replace(Environment.NewLine, "<br />"),
                Subject = model.Subject
            };

            if (model.SendCopyToMyself)
            {
                message.UserIds.Add(userDetails.Id);
            }

            _messageBus.DirectPublish(
                message: System.Text.Json.JsonSerializer.SerializeToElement(message),
                queueName: MessageQueues.MessageDelivery
             );

        }

        public async Task SetUserSiteAsync(int siteId, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, 0, true);

            var userDetails = await _dbContext.User.FirstOrDefaultAsync(x => x.UnixId == user.UnixId);

            if (userDetails == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);
            }

            if (!userDetails.Permissions.Any(p => p.SiteId == siteId))
            {
                throw new DCPForbiddenException(ErrorMessages.NoUserAccess);
            }

            userDetails.CurrentSiteId = siteId;
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateUserSettingsAsync(UserSettingsDTO model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER,
                Permissions.Global.GUEST,
                Permissions.Global.ADMIN
            }, user, 0, true);

            var userDetails = await _dbContext.User.FirstOrDefaultAsync(x => x.UnixId == user.UnixId);

            if (userDetails == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);
            }

            userDetails.TimeFormat = model.TimeFormat;
            userDetails.DateFormat = model.DateFormat;

            await _dbContext.SaveChangesAsync();
        }
    }
}
