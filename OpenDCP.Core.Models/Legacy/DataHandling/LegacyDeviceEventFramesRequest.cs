using OpenDCP.Framework.Configuration.Model.Entities.Site;

namespace OpenDCP.Core.Models.Legacy.DataHandling
{
#pragma warning disable CS1591
#pragma warning disable CS8618
    public class LegacyDeviceEventFramesRequest
    {
        public string DeviceWebId { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool IsActive { get; set; }
        public LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
    }
#pragma warning restore CS1591
#pragma warning restore CS8618

}
