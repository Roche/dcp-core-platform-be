using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents a data type required by a module,
    /// including its mandatory status and whether multiple selections are allowed.
    /// </summary>
    public class RequiredDataTypeDTO
    {
        /// <summary>
        /// Gets or sets the type of the data.
        /// </summary>
        public DataType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this data type is mandatory.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool Mandatory { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether multiple selections are allowed for this data type.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool AllowMultiselection { get; set; } = false;
    }

}
