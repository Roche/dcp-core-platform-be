using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.DAL.Entities
{
    public class SecuritySystemLog
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [StringLength(150)]
        [Description("ClientId")]
        public required string ClientId { get; set; }
        public SecurityAction Action { get; set; }
        [Description("Timestamp in UTC")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    }
}
