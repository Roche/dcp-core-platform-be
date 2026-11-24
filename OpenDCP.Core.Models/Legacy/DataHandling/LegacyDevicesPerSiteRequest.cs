using OpenDCP.Framework.Configuration.Model.Entities.Site;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.DataHandling
{
    public class LegacyDevicesPerSiteRequest
    {
        public string Query { get; set; } = string.Empty;
        public LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618