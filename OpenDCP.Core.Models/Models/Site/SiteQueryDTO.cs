namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents a query object used to filter data by one or more site identifiers.
    /// </summary>
    public class SiteQueryDTO
    {
        /// <summary>
        /// Gets or sets the collection of site identifiers to include in the query.
        /// </summary>
        public List<int> SiteId { get; set; } = new List<int>();
        /// <summary>
        /// Gets or sets the collection of site names to include in the query.
        /// </summary>
        public List<string> SiteName { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets the collection of site short names to include in the query.
        /// </summary>
        public List<string> ShortName { get; set; } = new List<string>();
    }

}
