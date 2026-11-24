using System.ComponentModel.DataAnnotations;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacySiteDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }

        [StringLength(250)]
        public string ShortName { get; set; }

        [StringLength(250)]
        public string TimeZone { get; set; }
        public bool IsEnable { get; set; }
        public string Reason { get; set; }
        public byte SiteType { get; set; }
        public List<LegacyModuleDTO> Modules { get; set; }
        public List<LegacyDataSourceDTO> DataSources { get; set; }
        public List<LegacyConnectionBridgeDTO> ConnectionBridges { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618