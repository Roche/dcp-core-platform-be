using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.DAL.Entities
{
    public class DashboardTab
    {
        public DashboardTab()
        {
            DashboardExport = new HashSet<DashboardExport>();
        }
        public virtual ICollection<DashboardExport> DashboardExport { get; set; }
        [Description("UniqueId")]
        [Key]
        public Guid Id { get; set; }
        [Description("User defined name of the dashboard tab")]
        [StringLength(250)]
        public required string Name { get; set; }
        [Description("Relation to the site where the dashboard tab is linked to")]
        public int SiteId { get; set; }
        [Description("Version identifier, used in import/export to load correct version")]
        public int Version { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; set; }
        public DateTime SysEndTime { get; set; }
    }
}
