using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Controllers;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.Security.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace OpenDCP.Core.API.Controllers
{
    /// <summary>
    /// Base controller for all API controllers in the DCP Core API.
    /// Provides common functionality such as retrieving the current user context
    /// and validating internal microservice requests.
    /// </summary>
    [ApiController]
    public abstract class BaseController : DCPFrameworkBaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        protected BaseController(IConfiguration configuration) : base(configuration)
        {

        }

        /// <summary>
        /// Retrieves the current user context from the claims in the HTTP request.
        /// </summary>
        /// <remarks>
        /// If <c>Debug:Enabled</c> is set to true in the configuration, a default user context
        /// is returned for development purposes.
        /// Otherwise, the method reads claims to build a <see cref="UserContextDTO"/>
        /// </remarks>
        /// <returns>A <see cref="UserContextDTO"/> representing the current user.</returns>
        protected UserContextDTO GetUserContext()
        {
            if (bool.TryParse(_configuration["Debug:Enabled"], out var isDebugEnabled) 
                && isDebugEnabled
                && !(User.Identity?.IsAuthenticated ?? false))
            {
                if(!_configuration.GetSection("Debug:User").Exists())
                {
                    return new UserContextDTO();
                }
                else
                {
                    return new UserContextDTO
                    {
                        UserId = _configuration.GetValue<int>("Debug:User:UserId"),
                        UnixId = _configuration.GetValue<string>("Debug:User:UnixId") ?? throw new DCPInternalServerErrorException("Debug user configuration not found"),
                        UserDisplayName = _configuration.GetValue<string>("Debug:User:DisplayName") ?? throw new DCPInternalServerErrorException("Debug user configuration not found"),
                        PersistedGrantType = (byte)PersistedGrantsType.SSO,
                        Permissions = _configuration.GetSection("Debug:User:Permissions").Get<List<PermissionDTO>>() ?? throw new DCPInternalServerErrorException("Debug user configuration not found")
                    };
                }
            }

            var claims = User.Claims;

            var permissions = claims
                .Where(c => c.Type == "permissions")
                .Select(c =>
                {
                    var perm = JsonSerializer.Deserialize<PermissionDTO>(c.Value);
                    if (perm == null)
                        throw new InvalidOperationException("Invalid or empty permission claim found.");
                    return perm;
                })
                .ToList();

            var user = new UserContextDTO
            {
                UnixId = GetRequiredClaimValue(claims, "userName"),
                //Server-to-server token don't have this property set
                UserDisplayName = GetClaimValue(claims, "displayName"),
                //UserId = int.Parse(claims.FirstOrDefault(x => x.Type == "userId").Value),
                UserId = int.Parse(GetRequiredClaimValue(claims, "currentLoggedUserId")),
                Permissions = permissions,
                PersistedGrantType = byte.Parse(GetRequiredClaimValue(claims,"persistedGrantsType"))
            };

            return user;
        }

        /// <summary>
        /// Validates that the current HTTP request is an internal request from a trusted microservice.
        /// </summary>
        /// <remarks>
        /// The method checks the following:
        /// <list type="bullet">
        /// <item>If <c>Debug:Enabled</c> configuration is true, all requests are allowed.</item>
        /// <item>If <c>Debug:Enabled</c> is false, the request must contain the header <c>X-DCPMicroservice-Key</c> 
        /// with a value matching <c>Headers:Microservice:Value</c> from configuration.</item>
        /// </list>
        /// If the request does not meet these requirements, a <see cref="DCPUnauthorizedException"/> is thrown.
        /// </remarks>
        /// <exception cref="DCPUnauthorizedException">
        /// Thrown when the request is not authorized as an internal microservice request.
        /// </exception>
        protected void ValidateAsInternalRequest(string? microserviceKey)
        {
            bool debugEnabled;
            bool.TryParse(_configuration["Debug:Enabled"], out debugEnabled);

            //Allow all requests in debug mode
            if (debugEnabled)
                return; 

            var expectedHeaderValue = _configuration["Headers:Microservice:Value"] ?? string.Empty;

            if (!string.Equals(
                    microserviceKey,
                    expectedHeaderValue,
                    StringComparison.OrdinalIgnoreCase)
                )
            {
                throw new DCPUnauthorizedException("Internal endpoint");
            }
        }

        private static string GetRequiredClaimValue(IEnumerable<Claim> claims, string type)
        {
            var claim = claims.FirstOrDefault(c => c.Type == type)?.Value;
            if (string.IsNullOrWhiteSpace(claim))
                throw new InvalidOperationException($"Required claim '{type}' is missing or empty.");
            return claim;
        }

        private static string GetClaimValue(IEnumerable<Claim> claims, string type)
        {
            return claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;
        }
    }
}
