namespace OpenDCP.Core.Models.Models.Authentication
{
    /// <summary>
    /// Represents a single user entry from a auth provider configuration file, including credentials, personal information, and group memberships.
    /// </summary>
    public class ConfigurationFileItemDTO
    {
        /// <summary>
        /// Gets or sets the unique Unix identifier (username) for the user.
        /// </summary>
        public required string UnixId { get; set; }

        /// <summary>
        /// Gets or sets the password for the user.
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// Gets or sets the first name of the user.
        /// Defaults to an empty string if not provided.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name of the user.
        /// Defaults to an empty string if not provided.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address of the user.
        /// May be <c>null</c> if not specified.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the list of groups the user belongs to.
        /// Must contain at least one group.
        /// </summary>
        public required List<string> Groups { get; set; } = new List<string>();
    }

}
