namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Defines OAuth 2.0 and OpenID Connect grant type identifiers
    /// as specified by the OAuth 2.0 and related RFC standards.
    /// </summary>
    /// <remarks>
    /// These values are used in token requests (e.g. <c>grant_type</c>)
    /// and are represented as strings to remain fully compatible
    /// with extensible OAuth specifications.
    /// </remarks>
    public static class OAuthGrantTypes
    {
        /// <summary>
        /// Authorization Code grant type.
        /// </summary>
        /// <remarks>
        /// Used by confidential and public clients to exchange an authorization
        /// code for an access token after user authentication.
        /// </remarks>
        public const string AuthorizationCode = "authorization_code";

        /// <summary>
        /// Client Credentials grant type.
        /// </summary>
        /// <remarks>
        /// Used by machine-to-machine scenarios where no user context
        /// is involved and the client authenticates using its own credentials.
        /// </remarks>
        public const string ClientCredentials = "client_credentials";

        /// <summary>
        /// Refresh Token grant type.
        /// </summary>
        /// <remarks>
        /// Used to obtain a new access token using a previously issued
        /// refresh token without requiring user interaction.
        /// </remarks>
        public const string RefreshToken = "refresh_token";

        /// <summary>
        /// JWT Bearer grant type.
        /// </summary>
        /// <remarks>
        /// Defined in RFC 7523, this grant type allows a client to exchange
        /// a JSON Web Token (JWT) for an OAuth 2.0 access token.
        /// </remarks>
        public const string JwtBearer = "urn:ietf:params:oauth:grant-type:jwt-bearer";
    }
}
