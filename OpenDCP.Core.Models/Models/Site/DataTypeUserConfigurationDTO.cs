using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the user-specific configuration of a data type for a given datasource.
    /// </summary>
    public class DataTypeUserConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the optional unique identifier of the user data type configuration.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the type of data.
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the datasource associated with this data type configuration.
        /// </summary>
        public int DatasourceId { get; set; }
    }

}
