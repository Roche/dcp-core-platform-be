namespace OpenDCP.Core.Models.Models.Authentication
{
    /// <summary>
    /// Represents the parameters used to request or refresh an authentication token.
    /// </summary>
    public class TokenQueryDTO
    {
        /// <summary>
        /// Gets or sets the type of grant requested (e.g., "password", "client_credentials", "refresh_token").
        /// </summary>
        public required string grant_type { get; set; }

        /// <summary>
        /// Gets or sets the client identifier. Required for client credentials or authorization code grants.
        /// </summary>
        public string? clientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret. Required for client credentials or authorization code grants.
        /// </summary>
        public string? clientSecret { get; set; }

        /// <summary>
        /// Gets or sets the refresh token used to obtain a new access token.
        /// Only required for the "refresh_token" grant type.
        /// </summary>
        public string? refresh_token { get; set; }
    }

}
