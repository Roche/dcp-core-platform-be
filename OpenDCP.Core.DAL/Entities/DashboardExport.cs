using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDCP.Core.DAL.Entities
{
    public class DashboardExport
    {
        [Description("UniqueId")]
        [Key]
        public int Id { get; set; }
        [Description("System generated GUID used for sharing")]
        public Guid DashboardExportGuid { get; set; }
        [Description("Relation to the user performing the export")]
        public int UserId { get; set; }
        [Description("Relation to the site from where the dashboard is exported")]
        public int SiteId { get; set; }
        public required DashboardTab DashboardTab { get; set; }
        [Description("Unique id of the dashboard tab which was exported")]
        [ForeignKey("DashboardTab")]
        public Guid DashboardTabId { get; set; }
        [Description("The version")]
        public int DashboardTabVersion { get; set; }
        [Description("Datetime of the export in UTC")]
        public DateTime ExportDate { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; set; }
        public DateTime SysEndTime { get; set; }
    }
}
