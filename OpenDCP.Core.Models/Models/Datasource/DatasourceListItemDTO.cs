using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.Datasource
{
    /// <summary>
    /// Represents a site item in a list, providing essential information about the site.
    /// </summary>
    public class DatasourceListItemDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the datasource.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the full name of the datasource.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the datasource.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="SiteType"/> enumeration to classify the site.
        /// </remarks>
        public string ConnectionType { get; set; }
    }
}
