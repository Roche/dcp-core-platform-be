using OpenDCP.Framework.Configuration.Model.Responses.CalculationNode;

namespace OpenDCP.Core.Models.Models.CalculationNode
{
    /// <summary>
    /// Represents a response containing version-specific information
    /// for a calculation node.
    /// </summary>
    public class CalculationNodeVersionResponse : CalculationNodeResponse
    {
        /// <summary>
        /// Gets or sets the version details of the calculation node.
        /// </summary>
        public CalculationNodeVersionDTO? Version { get; set; }
    }

}
