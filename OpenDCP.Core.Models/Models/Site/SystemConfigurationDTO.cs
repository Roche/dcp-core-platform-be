using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Framework.FormBuilder.Models;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the overall system configuration, including modules, datasource connection types, and supported calculation node types.
    /// </summary>
    public class SystemConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the collection of system modules and their configurations.
        /// </summary>
        public List<ModuleSystemConfigurationDTO> Modules { get; set; }
            = new List<ModuleSystemConfigurationDTO>();

        /// <summary>
        /// Gets or sets the collection of available datasource connection types in the system.
        /// </summary>
        public List<DatasourceConnectionTypeDTO> DatasourceConnectionTypes { get; set; }
            = new List<DatasourceConnectionTypeDTO>();

        /// <summary>
        /// Gets or sets the collection of supported calculation node types, typically used for selection in the UI.
        /// </summary>
        public List<InputFieldOption> CalculationNodeTypes { get; set; }
            = new List<InputFieldOption>();

        /// <summary>
        /// Gets or sets the collection of supported connection types, typically used for selection in the UI.
        /// </summary>
        public List<InputFieldOption> ConnectionBridgeTypes { get; set; }
            = new List<InputFieldOption>();

        /// <summary>
        /// Gets or sets the collection of supported logical/business data types, typically used for selection in the UI.
        /// </summary>
        public List<InputFieldOption> DataTypes { get; set; }
            = new List<InputFieldOption>();
    }

}
