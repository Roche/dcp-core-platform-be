using OpenDCP.Core.Models.Models.Configuration;

namespace OpenDCP.Core.Models.Models.Module
{
    /// <summary>
    /// Represents the system-level configuration of a module, including its name, GxP compliance, required data types, and resources.
    /// </summary>
    public class ModuleSystemConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is subject to GxP (Good Practice) compliance requirements.
        /// </summary>
        public bool IsGxP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is a global scope, if false the module has a site relation.
        /// </summary>
        public bool IsGlobal { get; set; } = false;

        /// <summary>
        /// Gets or sets the collection of data types required by this module.
        /// </summary>
        public List<RequiredDataTypeDTO> DataTypes { get; set; } = new List<RequiredDataTypeDTO>();

        /// <summary>
        /// Gets or sets the collection of resources associated with this module.
        /// </summary>
        public List<ModuleResourceDTO> Resources { get; set; } = new List<ModuleResourceDTO>();
    }

}
