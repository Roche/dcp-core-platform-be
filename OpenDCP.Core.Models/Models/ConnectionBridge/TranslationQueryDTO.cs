using OpenDCP.Framework.Configuration.Model.Attributes;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;

namespace OpenDCP.Core.Models.Models.ConnectionBridge
{
    /// <summary>
    /// Represents a translation request based on a conenction bridge
    /// </summary>
    public class TranslationQueryDTO
    {
        /// <summary>
        /// The list of ids/words to be translated
        /// </summary>
        [ConnectionManager(RequestPipelineAction.Decode)]
        public List<string> Identifiers { get; set; } = new List<string>();
        /// <summary>
        /// Optional filter for the source data type.
        /// </summary>
        public DataType? SDataType { get; set; } = null;
        /// <summary>
        /// Optional filter for the target data type.
        /// </summary>
        public DataType? TDataType { get; set; } = null;
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
