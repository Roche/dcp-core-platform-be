using OpenDCP.Framework.Configuration.Model.Entities.Site;


namespace OpenDCP.Core.Models.Legacy.DataHandling
{
#pragma warning disable CS1591
#pragma warning disable CS8618
    public class LegacyAnalyticalDataByIdsRequest
    {
        public List<string> EventFrameIds { get; set; } = new List<string>();

        public List<string> AttributeIds { get; set; } = new List<string>();

        public LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
    }
#pragma warning restore CS1591
#pragma warning restore CS8618

}
