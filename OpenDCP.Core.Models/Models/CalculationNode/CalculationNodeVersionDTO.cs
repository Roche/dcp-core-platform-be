namespace OpenDCP.Core.Models.Models.CalculationNode
{
    /// <summary>
    /// Represents a specific version of a calculation node, including its associated packages.
    /// </summary>
    public class CalculationNodeVersionDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the calculation node
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the version identifier of the calculation node.
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Gets or sets the collection of package versions associated with this calculation node
        /// </summary>
        public List<CalculationNodePackageVersionDTO> Packages { get; set; }
            = new List<CalculationNodePackageVersionDTO>();
    }
}
