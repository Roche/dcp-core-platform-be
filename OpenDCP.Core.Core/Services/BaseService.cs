using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using OpenDCP.Framework.Hosting.Models;
using System.Linq;


namespace OpenDCP.Core.Core.Services
{
    public class BaseService
    {
        protected readonly DCPCoreDbContext _dbContext;
        protected readonly IConfiguration _configuration;
        protected readonly ILoggerFactory _factory;
        protected ILogger _logger;
        protected readonly IMemoryCache _memoryCache;

        public BaseService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _factory = factory;
            _logger = _factory.CreateLogger<BaseService>();
        }

        protected bool HasPermission(List<string> requiredPermissions, UserContextDTO user,int siteId, bool throwException)
        {
            if (Convert.ToBoolean(_configuration["DevelopmentMode"]))
            {
                return true;
            }

            bool hasPermission = false;

            if (user.PersistedGrantType == (byte)PersistedGrantsType.InternalMicroServiceClient
                || siteId == 0)
            {
                hasPermission = user.Permissions.Any(x => requiredPermissions.Contains(x.Permission));
            }
            else
            {
                hasPermission = user.Permissions.Any(x => requiredPermissions.Contains(x.Permission) 
                                                          && x.SiteId == siteId);
            }

            if (throwException && !hasPermission)
            {
                throw new DCPUnauthorizedException(ErrorMessages.PermissionDenied);
            }

            return hasPermission;
        }

        protected string GetEncryptionKey()
        {
            var key = _configuration["Secrets:EncryptionKey"];

            if (string.IsNullOrEmpty(key))
            {
                throw new DCPInternalServerErrorException(ErrorMessages.EncryptionKeyInvalid);
            }

            return key;
        }
    }
}
