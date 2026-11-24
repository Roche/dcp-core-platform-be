using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the configuration of a data type for a specific datasource.
    /// </summary>
    public class DataTypeConfigurationFormDTO
    {
        /// <summary>
        /// Gets or sets the optional unique identifier of the data type configuration.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the type of data.
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the datasource associated with this data type.
        /// </summary>
        public int DatasourceId { get; set; }
    }

}
