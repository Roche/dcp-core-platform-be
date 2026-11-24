namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents the parameters used to query modules, including site IDs, module name, resources, and filtering options.
    /// </summary>
    public class ModuleQueryDTO
    {
        /// <summary>
        /// Gets or sets the list of site IDs for which the module query should be performed.
        /// </summary>
        public List<int> SiteId { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the name of the module to query.
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional resource type to filter the query.
        /// </summary>
        public byte? Resource { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether disabled modules should be included in the result
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool IncludeDisabledModules { get; set; } = false;
    }

}
