using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents a datasource connection type and its supported data types.
    /// </summary>
    public class DatasourceConnectionTypeDTO
    {
        /// <summary>
        /// Gets or sets the display label of the datasource connection type.
        /// </summary>
        public required string Label { get; set; }

        /// <summary>
        /// Gets or sets the underlying value that identifies the datasource connection type.
        /// The value type may vary depending on the datasource implementation.
        /// </summary>
        public required dynamic Value { get; set; }

        /// <summary>
        /// Gets or sets the collection of data types supported by this datasource connection type.
        /// </summary>
        public List<DataType> AvailableDataTypes { get; set; }
            = new List<DataType>();
    }
}
