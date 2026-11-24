using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents a site item in a list, providing essential information about the site.
    /// </summary>
    public class SiteListItemDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the site.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the full name of the site.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the site.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="SiteType"/> enumeration to classify the site.
        /// </remarks>
        public SiteType SiteType { get; set; }

        /// <summary>
        /// Gets or sets the short name or abbreviation of the site.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the time zone of the site, typically in IANA or Windows format (e.g., "Europe/Berlin").
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the site is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }
    }

}
