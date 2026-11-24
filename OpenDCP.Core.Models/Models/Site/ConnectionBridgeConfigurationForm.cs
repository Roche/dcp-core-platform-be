using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the configuration form used to define a connection bridge
    /// between a source connection and a target connection.
    /// </summary>
    public class ConnectionBridgeConfigurationForm
    {
        /// <summary>
        /// Gets or sets the unique identifier of the connection bridge.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the bridge type identifier.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the source connection.
        /// </summary>
        public required int SConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the data type of the source connection.
        /// </summary>
        public required DataType SConnectionType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target connection.
        /// </summary>
        public required int TConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the data type of the target connection.
        /// </summary>
        public required DataType TConnectionType { get; set; }

        /// <summary>
        /// Gets or sets the display name of the connection bridge.
        /// </summary>
        public required string Name { get; set; }

    }

}
