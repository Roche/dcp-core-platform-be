using OpenDCP.Framework.Configuration.Model.Entities.Site;

#pragma warning disable CS1591
#pragma warning disable CS8618
namespace OpenDCP.Core.Models.Legacy.DataHandling
{
    public class LegacyShowSignalsRequest
    {
        public List<string> SensorsWebIds { get; set; } = new List<string>();
        public required LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
        public string? WebIdOfMaturity { get; set; }
        public required string StartTime { get; set; }
        public required string EndTime { get; set; }
        public string Interval { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618