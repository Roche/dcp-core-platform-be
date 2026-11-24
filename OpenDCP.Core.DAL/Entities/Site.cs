using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.AuditTrail.Attributes;

namespace OpenDCP.Core.DAL.Entities
{
    [AuditTable(SiteIdColumn = "Id")]
    [Table("Site")]
    public class Site
    {
        public Site()
        {
            Modules = new HashSet<Module>();
            Datasources = new HashSet<DataSource>();
        }

        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Required]
        [StringLength(250)]
        [Description("User defined name of the site")]
        public required string Name { get; set; }

        [StringLength(250)]
        [Description("Short name of the site, used to link to RADA")]
        public required string ShortName { get; set; }

        [StringLength(250)]
        [Description("Timezone of the site, needed for timezone conversion")]
        public required string TimeZone { get; set; }
        [Description("Flag if the site can be currently accessed by users/services")]
        public bool IsEnable { get; set; }
        /// <summary>
        /// 1 - Drug Substance, 2 - Drug Product, 3 - Development, 4 - Small Molecules
        /// </summary>
        [Description("Enumeration of the site classification: DS, DP, etc.")]
        public SiteType SiteType { get; set; }
        [Description("Change reason entered by the user")]
        public required string Reason { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
        public virtual ICollection<Module> Modules { get; set; }
        public virtual ICollection<DataSource> Datasources { get; set; }

    }
}
