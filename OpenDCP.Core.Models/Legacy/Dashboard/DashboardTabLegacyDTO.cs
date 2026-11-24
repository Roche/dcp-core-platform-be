using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.Models.Legacy.Dashboard
{
    [Obsolete]
    public class DashboardTabLegacyDTO
    {
        public Guid ID { get; set; }
        [StringLength(250)]
        public string Name { get; set; }
        public int SiteId { get; set; }
        public int CurrentUserId { get; set; }
        public bool IsGxp { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? UserIDCreated { get; set; }
        public int? UserIDUpdated { get; set; }
    }
}
