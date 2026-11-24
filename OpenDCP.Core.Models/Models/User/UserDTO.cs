using OpenDCP.Core.Models.Models.Site;

namespace OpenDCP.Core.Models.Models.User
{
    /// <summary>
    /// Represents a user with identifying information, settings, and associated sites.
    /// </summary>
    public class UserDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the Unix ID (username) of the user.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string UnixId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the user.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's settings.
        /// </summary>
        public UserSettingsDTO Settings { get; set; } = new UserSettingsDTO();

        /// <summary>
        /// Gets or sets the list of sites associated with the user.
        /// </summary>
        /// <remarks>
        /// Initialized to an empty list by default to avoid null references.
        /// </remarks>
        public List<UserSiteDTO> Sites { get; set; } = new List<UserSiteDTO>();
        /// <summary>
        /// The siteId of the last user session (set during log-out) - used to initalize state
        /// </summary>
        public int? LastSessionSiteId { get; set; }
    }

}
