using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents the public (without auth) configuration details of a data source.
    /// </summary>
    public class DatasourcePublicConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the data source.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the data source.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the site to which the data source belongs.
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Gets or sets the name of the site associated with the data source.
        /// </summary>
        public required string SiteName { get; set; }

        /// <summary>
        /// Gets or sets the type of connection used by the data source.
        /// </summary>
        public required string ConnectionType { get; set; }

        /// <summary>
        /// Gets or sets the collection of data types provided by the data source.
        /// </summary>
        public List<DataType> DataTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the data source is validated for GxP purposes.
        /// </summary>
        public bool IsValidated { get; set; }
    }

}
