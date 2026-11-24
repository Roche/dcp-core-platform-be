namespace OpenDCP.Core.Models.Legacy.Site
{
#pragma warning disable CS1591
#pragma warning disable CS8618

    public class LegacySiteInfoDTO
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int SiteId { get; set; }
        public string TimeZone { get; set; }
        public bool IsEnable { get; set; }
        public byte SiteType { get; set; }
        public List<LegacyModuleDTO> Modules { get; set; }
        public List<LegacyDataSourceDTO> DataSources { get; set; }
    }
#pragma warning restore CS1591
#pragma warning restore CS8618
}
