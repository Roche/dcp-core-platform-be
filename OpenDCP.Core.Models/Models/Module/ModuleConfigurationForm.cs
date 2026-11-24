using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;

namespace OpenDCP.Core.Models.Models.Module
{
    /// <summary>
    /// Represents the configuration of a module, including its status, type, name, and associated data types.
    /// </summary>
    public class ModuleConfigurationForm
    {
        /// <summary>
        /// Gets or sets the unique identifier of the module configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module configuration has been validated.
        /// </summary>
        public bool IsValidated { get; set; }

        /// <summary>
        /// Gets or sets the type of the calculation node associated with the module.
        /// </summary>
        public CalculationNodeType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection of data type configurations associated with this module.
        /// </summary>
        public List<DataTypeConfigurationFormDTO> DataTypes { get; set; }
            = new List<DataTypeConfigurationFormDTO>();
    }

}
