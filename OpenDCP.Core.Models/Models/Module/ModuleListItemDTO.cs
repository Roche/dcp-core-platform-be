namespace OpenDCP.Core.Models.Models.Module
{
    /// <summary>
    /// Represents a lightweight data transfer object for a module entry.
    /// </summary>
    /// <remarks>
    /// Configuration data should not be extracted from this list
    /// </remarks>
    public class ModuleListItemDTO
    {

        /// <summary>
        /// Gets or sets the unique name of the module.
        /// </summary>
        /// <remarks>
        /// This value is required and is used to identify the module.
        /// </remarks>
        public required int Id { get; set; }

        /// <summary>
        /// Gets or sets the unique name of the module.
        /// </summary>
        /// <remarks>
        /// This value is required and is used to identify the module.
        /// </remarks>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the module is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }
    }
}
