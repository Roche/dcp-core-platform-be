using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.DAL.Entities
{
    public class AuditTrailAdminAction
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("The userId who created the entity")]
        public int? UserId { get; set; }
        [Description("Timestamp in UTC when the action was executed")]
        public DateTime? Timestamp { get; set; }
        public string? ActionConfiguration { get; set; }
        [Description("Change reason entered by the user")]
        public required string Reason { get; set; }
        [Description("The module which is impacted by the admin action")]
        public required string Module { get; set; }

        [Description("The exception(if any) occured during the processing of the admin action")]
        public string? Exception { get; set; }
    }
}
