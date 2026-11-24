using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents a site with its basic identifying information, type, and status.
    /// </summary>
    public class SiteDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the site.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the full name of the site.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the short name or abbreviation of the site.
        /// </summary>
        public required string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the time zone of the site, typically in IANA or Windows format (e.g., "Europe/Berlin").
        /// </summary>
        public required string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the type of the site.
        /// </summary>
        /// <remarks>
        /// Represented as a byte. Consider using a corresponding enumeration for clarity.
        /// </remarks>
        public SiteType SiteType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the site is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }
    }

}
