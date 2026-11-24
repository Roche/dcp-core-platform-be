using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace OpenDCP.Core.DAL.Entities
{
    public class PersistedGrant
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [StringLength(150)]
        [Description("Username of the login user")]
        public required string UnixId { get; set; }
        [Description("Creation datetime in UTC")]
        public DateTime CreationTime { get; set; }
        [Description("Expiration datetime of the session token (14 min)")]
        public DateTime Expiration { get; set; }
        public byte Type { get; set; }
    }
}
