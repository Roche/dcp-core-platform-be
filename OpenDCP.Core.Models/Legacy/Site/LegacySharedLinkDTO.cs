using System.Text.Json.Nodes;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacySharedLinkDTO
    {
        public JsonObject Link { get; set; }
        public string? Guid { get; set; }
        public string Title { get; set; }
        public List<LegacySharedLinkAliasDTO> ShareLinkAliasDetails { get; set; } = new List<LegacySharedLinkAliasDTO>();
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? UserIDCreated { get; set; }
        public int? UserIDUpdated { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618