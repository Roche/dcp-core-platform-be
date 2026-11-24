using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents a query for retrieving datasource data, including target sites, data types, and an optional module filter.
    /// </summary>
    public class DatasourceQueryDTO
    {
        /// <summary>
        /// Gets or sets the list of site IDs to query.
        /// </summary>
        /// <remarks>
        /// The list is initialized to an empty list by default to avoid null references.
        /// </remarks>
        public List<int> SiteId { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the list of data types to query.
        /// </summary>
        /// <remarks>
        /// The list is initialized to an empty list by default. Uses the <see cref="DataType"/> enumeration.
        /// </remarks>
        public List<DataType> DataType { get; set; } = new List<DataType>();

        /// <summary>
        /// Gets or sets an optional module name to filter the query.
        /// </summary>
        public string? Module { get; set; }
    }

}
