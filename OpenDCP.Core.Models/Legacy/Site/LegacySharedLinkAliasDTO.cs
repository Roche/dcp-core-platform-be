#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacySharedLinkAliasDTO
    {
        public int? ID { get; set; }
        public string? Name { get; set; }
        public Guid SharedLinkGuid { get; set; }
        public DateTime? DateCreated { get; set; }

        public DateTime? DateUpdated { get; set; }

        public int? UserIDCreated { get; set; }

        public int? UserIDUpdated { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618
