namespace OpenDCP.Core.Models.Legacy.Site
{
    [Obsolete]
#pragma warning disable CS1591
#pragma warning disable CS8618
    public class LegacyActiveModule
    {
        public int ID { get; set; }
        public required string ModuleName { get; set; }
        public string? RConnection { get; set; }
        public int SiteID { get; set; }
        public bool IsEnable { get; set; }
        public bool IsValidated { get; set; }
        public string? DataType { get; set; } = null;
    }
#pragma warning restore CS1591
#pragma warning restore CS8618
}
