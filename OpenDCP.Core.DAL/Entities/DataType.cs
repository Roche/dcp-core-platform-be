using OpenDCP.Framework.AuditTrail.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDCP.Core.DAL.Entities
{
    [AuditTable(
        SiteJoin = "LEFT JOIN (SELECT DISTINCT \"Id\", \"SiteId\" from public.\"DataSource__from_to\"('-infinity','infinity')) ds on ds.\"Id\" = t.\"DataSourceId\"",
        SiteIdColumn = "ds.\"SiteId\"",
        RelationFilterSql = "ds.\"SiteId\" = {RecordId}"
    )]
    public class DataType
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("A datatype, see the enumartion for translation")]
        public Framework.Configuration.Model.Constants.Datasource.DataType Type { get; set; }
        [Description("Name of the microservice/module consuming this datatype")]
        public required string ModuleName { get; set; }
        [ForeignKey("DataSource")]
        [Description("Relation to the datasource providing this data type for the module")]
        public int DataSourceId { get; set; }
        [Description("Change reason entered by the user")]
        public required string Reason { get; set; }
        public virtual DataSource? DataSource { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
