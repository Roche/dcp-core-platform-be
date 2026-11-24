namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents a site associated with a user, including its identification, name, time zone, and status.
    /// </summary>
    public class UserSiteDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the site.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the site.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the time zone of the site, typically in IANA or Windows format.
        /// </summary>
        public required string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the site is enabled for the user.
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
