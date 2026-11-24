using System.Text.Json.Nodes;

namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents the configuration settings for a connection bridge.
    /// </summary>
    public class ConnectionBridgeConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the connection bridge configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the source connection identifier.
        /// </summary>
        public int SConId { get; set; }

        /// <summary>
        /// Gets or sets the target connection identifier.
        /// </summary>
        public int TConId { get; set; }

        /// <summary>
        /// Gets or sets the strategy identifier used to determine 
        /// how the connection bridge should behave.
        /// </summary>
        /// <remarks>
        /// The value corresponds to a predefined strategy definition 
        /// interpreted by the consuming application.
        /// </remarks>
        public byte Strategy { get; set; }

        /// <summary>
        /// Gets or sets the JSON-based definition containing 
        /// the detailed configuration parameters for the connection bridge.
        /// </summary>
        /// <remarks>
        /// The structure of this object depends on the selected <see cref="Strategy"/> 
        /// and is interpreted dynamically at runtime.
        /// </remarks>
        public required JsonObject Definition { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection bridge configuration is GxP compliant.
        /// </summary>
        public bool IsGxP { get; set; }
    }
}