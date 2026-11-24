using OpenDCP.Framework.AuditTrail.Attributes;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDCP.Core.DAL.Entities
{
    [AuditTable(
        RelationFilterSql = "\"SiteId\" = {RecordId}")
    ]
    public class Module
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [StringLength(250)]
        [Description("Name of the module/microservice")]
        public required string Name { get; set; }
        [Description("Technical implementation type of the datasource e.g. PI used for interface resolution")]
        public CalculationNodeType Type { get; set; }
        [Description("Encrypted connection string to the calculation engine")]
        public required string ConnectionString { get; set; }
        [ForeignKey("Site")]
        [Description("Relation to the site, to enable site depended computation offloading")]
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        [Description("Flag if the module is currently enabled at the site")]
        public bool IsEnable { get; set; }
        [Description("Flag indicating the qualification status of the calculation engine")]
        public bool IsValidated { get; set; }
        [Description("Change reason entered by the user")]
        public required string Reason { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
