namespace OpenDCP.Core.Models.Models.Datasource
{
    /// <summary>
    /// Represents a query object used to filter data by one or more site identifiers.
    /// </summary>
    public class DatasourceQueryDTO
    {
        /// <summary>
        /// Gets or sets the collection of site identifiers to include in the query.
        /// </summary>
        public List<int> SiteId { get; set; } = new List<int>();

    }

}
