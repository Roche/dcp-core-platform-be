namespace OpenDCP.Core.Models.Models.CalculationNode
{
    /// <summary>
    /// Represents the version information of a calculation node package.
    /// </summary>
    public class CalculationNodePackageVersionDTO
    {
        /// <summary>
        /// Gets or sets the name of the calculation node package.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the version identifier of the calculation node package.
        /// </summary>
        public required string Version { get; set; }
    }
}
