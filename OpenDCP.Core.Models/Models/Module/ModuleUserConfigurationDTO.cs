using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.FormBuilder.Models;

namespace OpenDCP.Core.Models.Models.Module
{
    /// <summary>
    /// Represents the configuration of a module for a user, including its status, type, name, connection details, and data types.
    /// </summary>
    public class ModuleUserConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the optional unique identifier of the module configuration.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the optional site identifier of the module configuration, if null it's a global module configuration.
        /// </summary>
        public int? SiteId { get; set; }

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
        /// Gets or sets the connection string parameters or form values for this module.
        /// </summary>
        public List<FormValue> ConnectionString { get; set; } = new List<FormValue>();

        /// <summary>
        /// Gets or sets the collection of user-specific data type configurations for this module.
        /// </summary>
        public List<DataTypeUserConfigurationDTO> DataTypes { get; set; } = new List<DataTypeUserConfigurationDTO>();

        //ToDo add required when new FE version is deployed
        /// <summary>
        /// Gets or sets the reason for any changes made to the site configuration.
        /// </summary>
        public string ChangeReason { get; set; } = string.Empty;
    }

}
