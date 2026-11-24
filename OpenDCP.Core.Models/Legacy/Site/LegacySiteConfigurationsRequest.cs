#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacySiteConfigurationsRequest
    {
        public string? CurrentUser { get; set; }
        public int? SiteId { get; set; }
        public int? ModelId { get; set; }
        public bool? IsPrimary { get; set; }
        public byte? DataSourceType { get; set; }
        public string Module { get; set; }
        public List<int>? SiteIds { get; set; } = new List<int>();
        public byte? DataType { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618