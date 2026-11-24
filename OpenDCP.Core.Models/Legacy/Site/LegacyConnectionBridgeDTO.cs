#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacyConnectionBridgeDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public byte PrimaryConnectionDataType { get; set; }
        public int PrimaryConnectionDataSourcesId { get; set; }
        public byte SecondaryConnectionDataType { get; set; }
        public int SecondaryConnectionDataSourcesId { get; set; }
        public int SiteId { get; set; }
        public List<LegacyConnectionBridgeKeyMappingDTO> KeyMapping { get; set; }
        public string Reason { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618