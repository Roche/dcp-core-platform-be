namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents a resource/action/method associated with a module, including its location and type.
    /// </summary>
    public class ModuleResourceDTO
    {
        /// <summary>
        /// Gets or sets the URL or location of the module resource.
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// Gets or sets the type of the module resource.
        /// The meaning of the type is defined by the application (e.g., 0 = audit trail, 1 = image, etc.).
        /// </summary>
        public byte Type { get; set; }
    }

}
