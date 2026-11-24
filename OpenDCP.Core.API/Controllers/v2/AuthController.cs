using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Constants;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Nodes;
using System.Web;


namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides authentication and authorization endpoints for the DCP platform.
    /// </summary>
    /// <remarks>
    /// This controller handles authentication provider integration, user sign-in,
    /// token exchange, token refresh, and token validation operations.
    /// </remarks>
    [Route("/api/v2/")]
    public class AuthController : BaseController
    {
        private readonly ITokenService _tokenService;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="tokenService">Service responsible for generating and managing tokens.</param>
        /// <param name="configuration">Application configuration settings.</param>
        /// <param name="authenticationProvider">Authentication provider used for login and request parsing.</param>
        /// <param name="userService">Used for storing user settings on log-out</param>

        public AuthController(
            ITokenService tokenService,
            IConfiguration configuration,
            IAuthenticationProvider authenticationProvider,
            IUserService userService)
            : base(configuration)
        {
            _authenticationProvider = authenticationProvider;
            _tokenService = tokenService;
            _userService = userService;
        }

        /// <summary>
        /// Retrieves the login URL for the authentication provider.
        /// </summary>
        /// <remarks>
        /// This endpoint can be used by the frontend to re-direct to the correct authentication provider to handle the login process
        /// The URL is generated using the current request context.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the login URL as a response.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        [Route("resource")]
        [Produces(typeof(string))]
        public IActionResult GetAuthProviderSettings()
        {
            var url = _authenticationProvider.LoginUrl();
            return Ok(url);
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <remarks>
        /// This endpoint is accessible without authentication, as indicated by the <see cref="AllowAnonymousAttribute"/>.
        /// It performs a sign-out operation and returns an acknowledgment response.
        /// </remarks>
        [HttpGet]
        [Route("sign-out")]
        [Route("/api/SSO/SSOSignOut")]
        [Authorize(AuthenticationSchemes = "JwtIgnoreExpiration")]
        public async Task<IActionResult> SignOut([FromQuery] int? siteId = null)
        {
            var user = GetUserContext();

            if (siteId.HasValue)
            {
                await _userService.SetUserSiteAsync(siteId.Value, user);
            }

            await _tokenService.RevokeUserTokensAsync(user.UnixId);
            var url = await _authenticationProvider.LogOutAsync();
            await _tokenService.LogUserActionAsync(user.UnixId, SecurityAction.Logout);

            return new JsonResult(url);
        }

        /// <summary>
        /// Exchanges a collect token from the Authorization header for an access token.
        /// </summary>
        /// <remarks>
        /// This endpoint expects a bearer token in the <c>Authorization</c> HTTP header.
        /// The provided collect token is exchanged for an access token using the token service.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the exchanged access token.
        /// </returns>
        [HttpGet]
        [Route("token/collect")]
        public async Task<IActionResult> CollectToken([FromHeader(Name = "Authorization")] string authorizationHeader)
        {
            // Get the Authorization header
            if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
            {
                var collectToken = authorizationHeader.Substring("Bearer ".Length).Trim();
                var accessToken = await _tokenService.ExchangeCollect4AccessToken(collectToken);

                return Ok(accessToken);
            }

            throw new DCPForbiddenException("Token could not be exchanged");
        }

        /// <summary>
        /// Handles user sign-in and generates an authentication token.
        /// </summary>
        /// <remarks>
        /// This endpoint supports both GET and POST requests
        /// The request data is extracted and used to generate a token for the user.
        /// </remarks>
        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        [Route("sign-in")]
        [Route("/api/SSO")]
        [Produces(typeof(TokenResponseDTO))]
        public async Task<IActionResult> SignIn()
        {
            var stringData = _authenticationProvider.ExtractRequestData(Request);
            var token = await _tokenService.GenerateCollectToken(stringData);
            var platformUrl = _configuration["Host:App:Main"];

            return Redirect($"{platformUrl}/sso?token={token}");
        }

        /// <summary>
        /// Generates an authentication token for a trusted client certificate.
        /// </summary>
        /// <remarks>
        /// This endpoint accepts POST requests.
        /// The client certificate is extracted from the request and used to generate a token.
        /// </remarks>
        [HttpPost]
        [Route("cert/token")]
        [Produces(typeof(TokenResponseDTO))]
        [Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GenerateCertificateToken()
        {
            var cert = await HttpContext.Connection.GetClientCertificateAsync();

            if (cert == null) return Unauthorized();

            var token = await _tokenService.GenerateToken(cert.Thumbprint, PersistedGrantsType.Certificate);

            return Ok(token);
        }

        /// <summary>
        /// Generates or refreshes an authentication token based on the specified grant type.
        /// </summary>
        /// <remarks>
        /// This endpoint supports token operations, such as generating new tokens or refreshing existing ones.
        /// It is accessible without authentication, as denoted by the <see cref="AllowAnonymousAttribute"/>.
        /// The grant type and relevant parameters must be provided in the query string.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="TokenQueryDTO"/> object containing the required parameters for the token operation.
        /// The <c>grant_type</c> property determines the type of token operation to perform.
        /// </param>
        [HttpPost]
        [AllowAnonymous]
        [Route("token")]
        [Produces(typeof(TokenResponseDTO))]
        public async Task<IActionResult> Token([FromQuery] TokenQueryDTO model)
        {
            TokenResponseDTO result;

            switch (model.grant_type)
            {
                case OAuthGrantTypes.RefreshToken:
                    if (model.refresh_token is null)
                        throw new DCPBadRequestException("Refresh Token required");
                    result = await _tokenService.RefreshToken(model.refresh_token);
                    break;
                case OAuthGrantTypes.ClientCredentials:

                    if (model.clientId is null || model.clientSecret is null)
                        throw new DCPBadRequestException("ClientId and Client secret required");

                    var type = model.clientId.EndsWith("Client") ? PersistedGrantsType.InternalMicroServiceClient : PersistedGrantsType.APIGateWay;

                    result = await _tokenService.GenerateToken(model.clientId, model.clientSecret, type);
                    break;
                default:
                    throw new DCPBadRequestException();
            }
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Generates or refreshes an authentication token based on the specified grant type.
        /// </summary>
        /// <remarks>
        /// This endpoint supports token operations, such as generating new tokens or refreshing existing ones.
        /// It is accessible without authentication, as denoted by the <see cref="AllowAnonymousAttribute"/>.
        /// The grant type and relevant parameters must be provided in the query string.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="TokenQueryDTO"/> object containing the required parameters for the token operation.
        /// The <c>grant_type</c> property determines the type of token operation to perform.
        /// </param>
        [HttpPost]
        [AllowAnonymous]
        [Route("form/token")]
        [Produces(typeof(TokenResponseDTO))]
        public async Task<IActionResult> FormToken([FromForm] TokenQueryDTO model)
        {
            TokenResponseDTO result;

            switch (model.grant_type)
            {
                case OAuthGrantTypes.RefreshToken:
                    if (model.refresh_token is null)
                        throw new DCPBadRequestException("Refresh Token required");
                    result = await _tokenService.RefreshToken(model.refresh_token);
                    break;
                case OAuthGrantTypes.ClientCredentials:

                    if (model.clientId is null || model.clientSecret is null)
                        throw new DCPBadRequestException("ClientId and Client secret required");

                    var type = model.clientId.EndsWith("Client") ? PersistedGrantsType.InternalMicroServiceClient : PersistedGrantsType.APIGateWay;

                    result = await _tokenService.GenerateToken(model.clientId, model.clientSecret, type);
                    break;
                default:
                    throw new DCPBadRequestException();
            }
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }


        /// <summary>
        /// Generates or refreshes an authentication token based on the specified grant type.
        /// </summary>
        /// <remarks>
        /// This endpoint supports token operations, such as generating new tokens or refreshing existing ones.
        /// It is accessible without authentication, as denoted by the <see cref="AllowAnonymousAttribute"/>.
        /// The grant type and relevant parameters must be provided in the query string.
        /// </remarks>
        [HttpPost]
        [AllowAnonymous]
        [Route("/Token")]
        [Obsolete("Core-v2 migration - Move parameters to query")]
        [Produces(typeof(TokenResponseDTO))]
        public async Task<IActionResult> LegacyToken()
        {
            TokenResponseDTO result;
            TokenQueryDTO model;

            if (Request.ContentType == "application/x-www-form-urlencoded")
            {
                model = new TokenQueryDTO
                {
                    refresh_token = Request.Form["refresh_token"].ToString(),
                    clientSecret = Request.Form["clientSecret"].ToString(),
                    clientId = Request.Form["clientId"].ToString(),
                    grant_type = Request.Form["grant_type"].ToString() ?? throw new DCPBadRequestException(ErrorMessages.InvalidClient)
                };
            }
            else
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                var parsed = HttpUtility.ParseQueryString(body);

                model = new TokenQueryDTO
                {
                    refresh_token = parsed["refresh_token"],
                    clientSecret = parsed["clientSecret"],
                    clientId = parsed["clientId"],
                    grant_type = parsed["grant_type"] ?? throw new DCPBadRequestException(ErrorMessages.InvalidClient)
                };

            }

            switch (model.grant_type)
            {
                case OAuthGrantTypes.RefreshToken:
                    if (model.refresh_token is null)
                        throw new DCPBadRequestException("Refresh Token required");
                    result = await _tokenService.RefreshToken(model.refresh_token, TokenFormat.V1);
                    break;
                case OAuthGrantTypes.ClientCredentials:
                    if (model.clientId is null || model.clientSecret is null)
                        throw new DCPBadRequestException("ClientId and Client secret required");

                    var type = model.clientId.EndsWith("Client") ? PersistedGrantsType.InternalMicroServiceClient : PersistedGrantsType.APIGateWay;

                    result = await _tokenService.GenerateToken(model.clientId, model.clientSecret, type);
                    break;
                default:
                    throw new DCPBadRequestException();
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(result.Token);

            var jResult = new JsonObject
            {
                {".expires", jwtToken.ValidTo.ToString("r") },
                {".issued", DateTime.UtcNow.ToString("r") },
                {"access_token", result.Token },
                {"expires_in", (int)(jwtToken.ValidTo - DateTime.UtcNow).TotalSeconds },
                {"refresh_token", result.RefreshToken },
                {"token_type", "bearer" }
            };

            return Ok(jResult);
        }

        /// <summary>
        /// Validates a token, this can be used by the microservices to check if a token has been revoked
        /// </summary>
        [HttpGet]
        [Route("token/validate")]
        [Route("/api/PersistedGrants/TokenExpiration")]
        public IActionResult ValidateToken()
        {
            return Ok();
        }
    }
}
