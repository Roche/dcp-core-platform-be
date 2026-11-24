namespace OpenDCP.Core.Models.Models.CalculationNode
{
    /// <summary>
    /// Represents calculation node information for a specific site,
    /// including its associated modules.
    /// </summary>
    public class SiteCalculationNodeInfoDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the site.
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Gets or sets the name of the site.
        /// </summary>
        public required string SiteName { get; set; }

        /// <summary>
        /// Gets or sets the collection of calculation node information for the site's modules.
        /// </summary>
        public List<ModuleCalculationNodeInfoDTO> Modules { get; set; }
            = new List<ModuleCalculationNodeInfoDTO>();
    }

}
