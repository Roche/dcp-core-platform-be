using OpenDCP.Framework.AuditTrail.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDCP.Core.DAL.Entities
{
    [AuditTable(
        RelationFilterSql = "\"SiteId\" = {RecordId}")
    ]
    public class DataSource
    {
        public DataSource()
        {
            DataTypes = new HashSet<DataType>();
        }
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }

        [StringLength(250)]
        [Description("User defined name of the datasource")]
        public required string Name { get; set; }

        [Description("Technical implementation type of the datasource e.g. PI used for interface resolution")]
        public required string Type { get; set; }
        [Description("Encrypted connection string to the datasource")]
        public required string ConnectionString { get; set; }
        [ForeignKey("Site")]
        [Description("Relation to the site, where the datasource is assigned to")]
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        [Description("Validation status of the datasource")]
        public bool IsValidated { get; set; }
        [Description("Change reason entered by the user")]
        public required string Reason { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
        public virtual ICollection<DataType> DataTypes { get; set; }
    }
}
