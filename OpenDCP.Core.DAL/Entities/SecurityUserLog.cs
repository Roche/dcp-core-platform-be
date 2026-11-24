using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.DAL.Entities
{
    public class SecurityUserLog
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [StringLength(150)]
        public required string UnixId { get; set; }
        public SecurityAction Action { get; set; }
        [Description("Timestamp in UTC")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
