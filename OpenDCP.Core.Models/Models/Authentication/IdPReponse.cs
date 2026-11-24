namespace OpenDCP.Core.Models.Models.Authentication
{
    /// <summary>
    /// Represents the response received from an Identity Provider (IdP) after authentication.
    /// Contains user information, assertion details, and group memberships.
    /// </summary>
    public class IdPReponse
    {
        /// <summary>
        /// Gets or sets the status code of the IdP response, indicating success or failure.
        /// </summary>
        public required string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the IdP that issued the response.
        /// </summary>
        public required string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the SAML assertion or authentication token.
        /// </summary>
        public required string AssertionId { get; set; }

        /// <summary>
        /// Gets or sets the Unix identifier (username) of the authenticated user.
        /// </summary>
        public required string UnixId { get; set; }

        /// <summary>
        /// Gets or sets the first name of the authenticated user.
        /// </summary>
        public required string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the authenticated user.
        /// </summary>
        public required string LastName { get; set; }

        /// <summary>
        /// Gets or sets the email address of the authenticated user.
        /// May be <c>null</c> if no email is provided by the IdP.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the list of groups or roles the user belongs to, as provided by the IdP.
        /// </summary>
        public List<string> Groups { get; set; } = new List<string>();

    }
}
