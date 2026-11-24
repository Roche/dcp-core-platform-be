using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.ConnectionBridge
{
    /// <summary>
    /// Represents query parameters for an API request that resolves or filters
    /// source-to-target connection bridges within a module and site context.
    /// </summary>
    public class ConnectionBridgeQueryDTO
    {
        /// <summary>
        /// Optional filter for the source data type.
        /// </summary>
        public DataType? SDataType { get; set; } = null;

        /// <summary>
        /// Optional filter for the target data type.
        /// </summary>
        public DataType? TDataType { get; set; } = null;

        /// <summary>
        /// Identifies the site context for the query.
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Optional filter for the source connection identifier.
        /// </summary>
        public int? SConnectionId { get; set; } = null;

        /// <summary>
        /// Optional filter for the target connection identifier.
        /// </summary>
        public int? TConnectionId { get; set; } = null;
    }

}
