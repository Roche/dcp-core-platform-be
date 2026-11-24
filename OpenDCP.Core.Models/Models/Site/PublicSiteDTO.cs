namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the information for a site with is publicly exposed (without auth)
    /// </summary>
    public class PublicSiteDTO
    {
        /// <summary>
        /// The unique identifier of the site.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the site.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The short name of the site.
        /// </summary>
        public required string ShortName { get; set; }

        /// <summary>
        /// The time zone of the site, typically in IANA or Windows format (e.g., "Europe/Berlin").
        /// </summary>
        public required string TimeZone { get; set; }
    }
}
