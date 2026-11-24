using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.Security.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class TokenService : BaseService, ITokenService
    {
        private readonly IAuthenticationProvider _authProvider;


        public TokenService(IConfiguration configuration, IAuthenticationProvider authProvider, DCPCoreDbContext dbContext, IMemoryCache memoryCache, ILoggerFactory logger) :
            base(dbContext, configuration, logger, memoryCache)
        {
            _authProvider = authProvider;
        }

        public async Task<TokenResponseDTO> ExchangeCollect4AccessToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var unixId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            if (unixId is null)
                throw new DCPBadRequestException("UnixId could not be extracted");

            var data = await _dbContext.User
                .Where(x => x.UnixId == unixId)
                .Select(x => new
                {
                    x.Id,
                    x.Permissions,
                    x.DisplayName
                })
                .AsNoTracking()
                .FirstAsync();

            var accessToken = await FormatToken(unixId, data.Permissions, PersistedGrantsType.SSO, data.Id, data.DisplayName);
            var refreshToken = await GenerateRefreshToken(unixId, PersistedGrantsType.SSO);

            await RegisterTokenAsync(unixId, PersistedGrantsType.SSO, accessToken.ttl);

            var result = new TokenResponseDTO
            {
                Token = accessToken.token,
                RefreshToken = refreshToken.token,
            };

            return result;
        }

        public async Task<string> GenerateCollectToken(string tokenOrCredentials)
        {
            IdPReponse data;

            try
            {
                data = await _authProvider.AuthenticateAsync(tokenOrCredentials);
            }
            catch
            {
                return "notauthorized";
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, data.UnixId),
            };

            var key = GetSecurityKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var ttl = DateTime.UtcNow.AddMinutes(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["IdP:Settings:Issuer"],
                audience: _configuration["IdP:Settings:Audience:Id"],
                claims: claims,
                expires: ttl,
                signingCredentials: credentials
            );

            var permissions = data.Groups
                .Select(g => Group2Permission(g))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            permissions = MapPermissions2Site(permissions);

            await LogLogin(data, permissions);
            await RegisterTokenAsync(data.UnixId, PersistedGrantsType.SSO, ttl);
            await LogUserActionAsync(data.UnixId, SecurityAction.Login);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<TokenResponseDTO> GenerateToken(string tokenOrCredentials)
        {
            var data = await _authProvider.AuthenticateAsync(tokenOrCredentials);

            var permissions = data.Groups
                .Select(g => Group2Permission(g))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            permissions = MapPermissions2Site(permissions);
            var userId = await LogLogin(data, permissions);

            var token = await FormatToken(data.UnixId, permissions, PersistedGrantsType.SSO, userId, $"{data.LastName}, {data.FirstName}");
            var refreshToken = await GenerateRefreshToken(data.UnixId, PersistedGrantsType.SSO);
            await RegisterTokenAsync(data.UnixId, PersistedGrantsType.SSO, token.ttl);

            var result = new TokenResponseDTO
            {
                Token = token.token,
                RefreshToken = refreshToken.token,
            };

            return result;
        }

        public async Task<TokenResponseDTO> GenerateToken(string clientId, string clientSecret, PersistedGrantsType type)
        {
            TokenDefinitionDTO? tokenDefinition;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidClient);
            }

            if (type == PersistedGrantsType.APIGateWay)
            {
                tokenDefinition = await _dbContext.UserClient
                    .Where(x => x.ClientId == clientId
                                && x.Status == UserClientStatus.Active)
                    .Select(x => new TokenDefinitionDTO
                    {
                        ClientSecret = x.ClientSecret,
                        Type = type,
                        Permissions = x.Permissions
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (tokenDefinition is null)
                {
                    await LogSystemActionAsync(clientId, SecurityAction.RestTokenRequestFail);
                    throw new DCPBadRequestException(ErrorMessages.InvalidClient);
                }

            }
            else if (type == PersistedGrantsType.InternalMicroServiceClient)
            {
                var data = await _dbContext.Client
                   .Where(x => x.ClientId == clientId
                                && x.Active)
                   .Select(x => new
                   {
                       x.ClientSecret,
                       x.Permissions
                   })
                   .AsNoTracking()
                   .FirstOrDefaultAsync();

                if (data is null)
                {
                    await LogSystemActionAsync(clientId, SecurityAction.LoginFail);
                    throw new DCPBadRequestException(ErrorMessages.InvalidClient);
                }

                tokenDefinition = new TokenDefinitionDTO
                {
                    ClientSecret = data!.ClientSecret,
                    Type = type,
                    Permissions = data.Permissions.Select(p => new PermissionDTO
                    {
                        Module = Permissions.Placeholder.AGNOSTIC,
                        Permission = p,
                        SiteName = Permissions.Placeholder.GLOBAL
                    }).ToList()
                };
            }
            else
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidRefreshToken);
            }

            if (EncryptionHelper.Decrypt(tokenDefinition.ClientSecret, GetEncryptionKey()) != clientSecret)
            {
                await LogSystemActionAsync(clientId, SecurityAction.RestTokenRequestFail);
                throw new DCPUnauthorizedException(ErrorMessages.InvalidClient);
            }

            var token = await FormatToken(
                clientId,
                tokenDefinition.Permissions,
                tokenDefinition.Type,
                0,
                string.Empty
            );

            var refreshToken = await GenerateRefreshToken(clientId, tokenDefinition.Type);

            await LogSystemActionAsync(clientId, SecurityAction.RestTokenSuccessfully);

            var result = new TokenResponseDTO
            {
                Token = token.token,
                RefreshToken = refreshToken.token,
            };

            return result;
        }

        public async Task<TokenResponseDTO> GenerateToken(string certificateThumbprint, PersistedGrantsType type)
        {
            if (string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidCertificate);
            }

            var tokenDefinition = await _dbContext.TrustedCertificate
                .Where(x => x.CertificateThumbprint == certificateThumbprint && x.Active)
                .Select(x => new TokenDefinitionDTO
                {
                    Type = type,
                    Permissions = x.Permissions
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (tokenDefinition is null)
            {
                _logger.LogError(certificateThumbprint);
                await LogSystemActionAsync(certificateThumbprint, SecurityAction.RestTokenRequestFail);

                throw new DCPBadRequestException(ErrorMessages.InvalidCertificate);
            }

            var token = await FormatToken(
                certificateThumbprint,
                tokenDefinition.Permissions,
                tokenDefinition.Type,
                0,
                string.Empty);

            var refreshToken = await GenerateRefreshToken(certificateThumbprint, tokenDefinition.Type);

            await LogSystemActionAsync(certificateThumbprint, SecurityAction.RestTokenSuccessfully);

            return new TokenResponseDTO
            {
                Token = token.token,
                RefreshToken = refreshToken.token
            };
        }

        public async Task<TokenResponseDTO> RefreshToken(string refreshToken, TokenFormat format = TokenFormat.V2)
        {

            var token = await _dbContext.RefreshToken
                .Where(x => x.Token == refreshToken
                            && !x.IsRevoked
                            && x.ExpirationDate > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            /* Temporary disable this check to allow legacy support
            if (token == null)
            {
                throw new DCPUnauthorizedException(ErrorMessages.InvalidRefreshToken);
            }*/

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshToken);

            var typeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "persistedGrantsType")?.Value;

            if (!int.TryParse(typeClaim, out var type))
            {
                throw new InvalidOperationException("Invalid or missing persistedGrantsType claim.");
            }

            var unixId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            if (unixId is null)
                throw new DCPUnauthorizedException(ErrorMessages.InvalidRefreshToken);

            List<PermissionDTO>? permissions;
            var displayName = string.Empty;
            var userId = 0;

            if (type == (int)PersistedGrantsType.SSO)
            {
                var userDetails = await _dbContext.User
                    .Where(x => x.UnixId == unixId)
                    .Select(x => new
                    {
                        x.Permissions,
                        x.DisplayName,
                        x.Id,
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (userDetails is null)
                {
                    await LogUserActionAsync(unixId, SecurityAction.RefreshTokenRequestFail);
                    throw new DCPBadRequestException("User not found");
                }

                permissions = userDetails.Permissions;
                displayName = userDetails.DisplayName;
                userId = userDetails.Id;

                await LogUserActionAsync(unixId, SecurityAction.RefreshTokenSuccessfully);

            }
            else if (type == (int)PersistedGrantsType.APIGateWay)
            {
                permissions = await _dbContext.UserClient
                    .Where(x => x.ClientId == unixId)
                    .Select(x => x.Permissions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            else if (type == (int)PersistedGrantsType.InternalMicroServiceClient)
            {
                var client = await _dbContext.Client
                   .Where(x => x.ClientId == unixId)
                   .Select(x => x.Permissions)
                   .AsNoTracking()
                   .FirstOrDefaultAsync();

                if (client is null)
                {
                    await LogSystemActionAsync(unixId, SecurityAction.RefreshTokenRequestFail);
                    throw new DCPBadRequestException("Client not found");
                }

                permissions = client
                   .Select(p => new PermissionDTO
                   {
                       Module = Permissions.Placeholder.AGNOSTIC,
                       Permission = p,
                       SiteName = Permissions.Placeholder.GLOBAL
                   })
                   .ToList();

                await LogSystemActionAsync(unixId, SecurityAction.RefreshTokenSuccessfully);
            }
            else
            {
                throw new DCPBadRequestException(ErrorMessages.InvalidRefreshToken);
            }

            (string token, DateTime ttl) accessToken;

            if (format == TokenFormat.V1)
            {
                accessToken = await FormatLegacyToken(unixId, permissions ?? new List<PermissionDTO>(), (PersistedGrantsType)type, userId, displayName);
            }
            else
            {
                accessToken = await FormatToken(unixId, permissions ?? new List<PermissionDTO>(), (PersistedGrantsType)type, userId, displayName);
            }

            var rotatedRefreshToken = await GenerateRefreshToken(unixId, (PersistedGrantsType)type);
            await RegisterTokenAsync(unixId, (PersistedGrantsType)type, accessToken.ttl);

            var result = new TokenResponseDTO
            {
                Token = accessToken.token,
                RefreshToken = rotatedRefreshToken.token,
            };

            return result;
        }
        private async Task<int> LogLogin(IdPReponse data, List<PermissionDTO> permissions)
        {
            var user = await _dbContext.User
                .FirstOrDefaultAsync(u => u.UnixId == data.UnixId);

            if (user == null)
            {
                user = new User
                {
                    UnixId = data.UnixId,
                    //Set default user settings
                    TimeFormat = "hh:mm:ss a",
                    DateFormat = "MMM dd, yyyy",
                };

                await _dbContext.User.AddAsync(user);
            }

            user.FirstName = data.FirstName;
            user.LastName = data.LastName;
            user.DisplayName = $"{data.FirstName} {data.LastName}";
            user.Email = data.Email ?? string.Empty;
            user.LastLogin = DateTime.UtcNow;
            user.Permissions = permissions;

            await _dbContext.SaveChangesAsync();

            return user.Id;
        }
        private Task<(string token, DateTime ttl)> FormatToken(string unixId, List<PermissionDTO> permissions, PersistedGrantsType type,
            int userId, string displayName)
        {
            //Interactive users are getting a guest permission
            if (type == PersistedGrantsType.SSO)
            {
                permissions.Add(new PermissionDTO
                {
                    Module = Permissions.Placeholder.AGNOSTIC,
                    SiteName = Permissions.Placeholder.GLOBAL,
                    Permission = Permissions.Global.GUEST
                });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, unixId),
                new Claim("permissions", JsonSerializer.Serialize(permissions), JsonClaimValueTypes.JsonArray),
                new Claim("userId", userId.ToString(), ClaimValueTypes.Integer),
                new Claim("displayName", displayName),
                new Claim("persistedGrantsType", ((int)type).ToString(), ClaimValueTypes.Integer),
                //Legacy claims to be removed when all apps are using the new token
                new Claim("userName", unixId),
                new Claim("isGlobalAdmin", (PersistedGrantsType.InternalMicroServiceClient == type || permissions.Any(p => p.Permission == Permissions.Global.ADMIN && p.SiteName == Permissions.Placeholder.GLOBAL)).ToString()),
                new Claim("isGlobalApiUser", permissions.Any(p => p.Permission == Permissions.Global.APIUSER && p.SiteName == Permissions.Placeholder.GLOBAL).ToString()),
                new Claim("currentLoggedUserId", userId.ToString(), ClaimValueTypes.String)
            };

            var key = GetSecurityKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var ttl = DateTime.UtcNow.AddMinutes(14);

            var token = new JwtSecurityToken(
                issuer: _configuration["IdP:Settings:Issuer"],
                audience: _configuration["IdP:Settings:Audience:Id"],
                claims: claims,
                expires: ttl,
                signingCredentials: credentials
            );

            var result = (new JwtSecurityTokenHandler().WriteToken(token), ttl);

            return Task.FromResult(result);
        }

        [Obsolete]
        private Task<(string token, DateTime ttl)> FormatLegacyToken(string unixId, List<PermissionDTO> permissions, PersistedGrantsType type,
            int userId, string displayName)
        {
            //Interactive users are getting a guest permission
            if (type == PersistedGrantsType.SSO)
            {
                permissions.Add(new PermissionDTO
                {
                    Module = Permissions.Placeholder.AGNOSTIC,
                    SiteName = Permissions.Placeholder.GLOBAL,
                    Permission = Permissions.Global.GUEST
                });
            }

            var sites = _dbContext.Site
                .Select(x => new
                {
                    x.Name,
                    x.ShortName,
                    SiteId = x.Id,
                    x.TimeZone,
                    x.IsEnable
                })
                .Where(x => permissions.Select(p => p.SiteId).Contains(x.SiteId))
                .ToList();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, unixId),
                new Claim("permissions", JsonSerializer.Serialize(permissions), JsonClaimValueTypes.JsonArray),
                new Claim("userId", userId.ToString(), ClaimValueTypes.Integer),
                new Claim("displayName", displayName),
                new Claim("persistedGrantsType", ((int)type).ToString(), ClaimValueTypes.Integer),
                //Legacy claims to be removed when all apps are using the new token
                new Claim("userName", unixId),
                new Claim("sites", JsonSerializer.Serialize(sites), JsonClaimValueTypes.JsonArray),
                new Claim("isGlobalAdmin", (PersistedGrantsType.InternalMicroServiceClient == type || permissions.Any(p => p.Permission == Permissions.Global.ADMIN && p.SiteName == Permissions.Placeholder.GLOBAL)).ToString()),
                new Claim("isGlobalApiUser", permissions.Any(p => p.Permission == Permissions.Global.APIUSER && p.SiteName == Permissions.Placeholder.GLOBAL).ToString()),
                new Claim("currentLoggedUserId", userId.ToString(), ClaimValueTypes.String)
            };

            var key = GetSecurityKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var ttl = DateTime.UtcNow.AddMinutes(14);

            var token = new JwtSecurityToken(
                issuer: _configuration["IdP:Settings:Issuer"],
                audience: _configuration["IdP:Settings:Audience:Id"],
                claims: claims,
                expires: ttl,
                signingCredentials: credentials
            );

            var result = (new JwtSecurityTokenHandler().WriteToken(token), ttl);

            return Task.FromResult(result);
        }
        private async Task<(string token, DateTime ttl)> GenerateRefreshToken(string identifer, PersistedGrantsType type)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, identifer),
                new Claim("persistedGrantsType", ((int)type).ToString())
            };

            var ttl = DateTime.UtcNow.AddDays(7);
            var key = GetSecurityKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["IdP:Settings:Issuer"],
                audience: _configuration["IdP:Settings:Audience:Id"],
                claims: claims,
                expires: ttl,
                signingCredentials: credentials
            );

            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            var token = await _dbContext.RefreshToken
                .Where(x => x.UserIdentifier == identifer)
                .FirstOrDefaultAsync();

            if (token == null)
            {
                token = new RefreshToken
                {
                    UserIdentifier = identifer,
                };

                await _dbContext.RefreshToken.AddAsync(token);
            }

            token.ExpirationDate = ttl;
            token.IsRevoked = false;
            token.Token = refreshTokenString;

            await _dbContext.SaveChangesAsync();

            return (token.Token, token.ExpirationDate);
        }
        private async Task RegisterTokenAsync(string unixId, PersistedGrantsType type, DateTime ttl)
        {
            var grant = new PersistedGrant
            {
                UnixId = unixId,
                Expiration = ttl,
                CreationTime = DateTime.UtcNow,
                Type = (byte)type
            };

            await _dbContext.PersitedGrant.AddAsync(grant);
            await _dbContext.SaveChangesAsync();
        }
        private List<PermissionDTO> MapPermissions2Site(List<PermissionDTO> permissions)
        {
            var sites = _dbContext.Site
                .Select(s => new
                {
                    s.ShortName,
                    s.Id
                })
                .AsNoTracking()
                .ToList();

            permissions = permissions
                .GroupJoin(
                    sites,
                    (p) => p.SiteName,
                    (s) => s.ShortName,
                    (p, siteGroup) => new { p, siteGroup })
                .SelectMany(
                    x => x.siteGroup.DefaultIfEmpty(),
                        (x, s) =>
                        new PermissionDTO
                        {
                            SiteName = x.p.SiteName,
                            Module = x.p.Module,
                            Permission = x.p.Permission,
                            SiteId = s?.Id ?? 0
                        }
                ).ToList();

            return permissions;
        }
        private PermissionDTO? Group2Permission(string groupName)
        {
            // Global module roles
            Match match = Regex.Match(
                groupName, _configuration["IdP:RoleRegex:GlobalModule"]!,
                RegexOptions.None,
                TimeSpan.FromMilliseconds(100)
            );

            if (match.Success)
            {
                return new PermissionDTO
                {
                    Permission = $"{match.Groups["Module"].Value.ToUpper()}{match.Groups["Permission"].Value.ToUpper()}",
                    SiteName = Permissions.Placeholder.GLOBAL,
                    SiteId = 0,
                    Module = match.Groups["Module"].Value
                };
            }

            //Global roles
            match = Regex.Match(
                groupName, _configuration["IdP:RoleRegex:Global"]!,
                RegexOptions.None,
                TimeSpan.FromMilliseconds(100)
            );

            if (match.Success)
            {
                return new PermissionDTO
                {
                    Permission = $"{Permissions.Placeholder.PLATFORM}{match.Groups["Permission"].Value.ToUpper()}",
                    SiteName = Permissions.Placeholder.GLOBAL,
                    SiteId = 0,
                    Module = Permissions.Placeholder.AGNOSTIC
                };
            }

            //Site module agnostic roles
            match = Regex.Match(
                groupName,
                _configuration["IdP:RoleRegex:Site"]!,
                RegexOptions.None,
                TimeSpan.FromMilliseconds(100)
            );

            if (match.Success)
            {
                return new PermissionDTO
                {
                    Permission = match.Groups["Permission"].Value.ToUpper(),
                    SiteName = match.Groups["SiteShort"].Value,
                    Module = Permissions.Placeholder.AGNOSTIC
                };
            }

            //Site and module specifc roles
            match = Regex.Match(groupName, _configuration["IdP:RoleRegex:Module"]!, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            if (match.Success)
            {
                return new PermissionDTO
                {
                    Permission = $"{match.Groups["Module"].Value.ToUpper()}{match.Groups["Permission"].Value.ToUpper()}",
                    Module = match.Groups["Module"].Value,
                    SiteName = match.Groups["SiteShort"].Value,
                };
            }

            return null;
        }
        private SymmetricSecurityKey GetSecurityKey()
        {
            var secret = _configuration["IdP:Settings:Audience:Secret"]
                ?? throw new DCPInternalServerErrorException("Missing Configuration");

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)); // NOSONAR - the secret is injected on runtime not part of the code base
        }

        public Task<bool> IsAllowedAsync(string unixId)
        {
            return _dbContext.PersitedGrant
                .AnyAsync(x => x.UnixId == unixId
                                && x.Expiration > DateTime.UtcNow);
        }

        public async Task RevokeUserTokensAsync(string unixId)
        {
            var grants = await _dbContext.PersitedGrant
                .Where(x => x.UnixId == unixId)
                .ToListAsync();

            if (grants.Count > 0)
            {
                _dbContext.PersitedGrant.RemoveRange(grants);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task LogUserActionAsync(string unixId, SecurityAction action)
        {
            await _dbContext.SecurityUserLog.AddAsync(
                new SecurityUserLog
                {
                    UnixId = unixId,
                    Action = action
                }
            );
            await _dbContext.SaveChangesAsync();
        }

        private async Task LogSystemActionAsync(string clientId, SecurityAction action)
        {
            await _dbContext.SecuritySystemLog.AddAsync(
                new SecuritySystemLog
                {
                    ClientId = clientId,
                    Action = action
                }
            );
            await _dbContext.SaveChangesAsync();
        }
    }
}
