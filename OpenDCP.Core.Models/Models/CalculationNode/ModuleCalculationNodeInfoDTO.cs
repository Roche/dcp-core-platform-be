using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;

namespace OpenDCP.Core.Models.Models.CalculationNode
{
    /// <summary>
    /// Represents calculation node information for a specific module, including its type, version, and associated package.
    /// </summary>
    public class ModuleCalculationNodeInfoDTO
    {
        /// <summary>
        /// Gets or sets the type of the calculation node.
        /// </summary>
        public CalculationNodeType Type { get; set; }

        /// <summary>
        /// Gets or sets the version of the calculation node.
        /// </summary>
        public string? NodeVersion { get; set; }

        /// <summary>
        /// Gets or sets the name of the module associated with the calculation node.
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// Gets or sets the version of the package associated with the calculation node module.
        /// </summary>
        public string? PackageVersion { get; set; }
    }

}
