using System.Text.Json.Serialization;


namespace OpenDCP.Core.Models.Models.Authentication
{
    /// <summary>
    /// Represents the response returned when requesting an authentication token,
    /// including the access token and optional refresh token.
    /// </summary>
    public class TokenResponseDTO
    {
        /// <summary>
        /// Gets or sets the access token issued by the authentication server.
        /// This token is used to authenticate API requests.
        /// </summary>
        [JsonPropertyName("access_token")]
        public required string Token { get; set; }

        /// <summary>
        /// Gets or sets the refresh token issued by the authentication server.
        /// This token can be used to obtain a new access token when the current one expires.
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; }
    }

}
