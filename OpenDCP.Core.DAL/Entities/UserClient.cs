using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Framework.Security.Models;
using System.ComponentModel.DataAnnotations.Schema;
using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.DAL.Entities
{
    public class UserClient
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Required]
        [Description("ClientId used for authentication for the REST access")]
        public required string ClientId { get; set; }
        [Required]
        [Description("Hashed secret, for REST client authentication")]
        public required string ClientSecret { get; set; }
        [Required]
        [StringLength(200)]
        [Description("User defined client name for client identification by the user")]
        public required string Name { get; set; }
        [StringLength(400)]
        [Description("Optional further information for client description")]
        public string Description { get; set; } = string.Empty;
        [Description("Status of the clientIds, disable clients can't be used")]
        public UserClientStatus Status { get; set; }
        [Description("The permissions granted by the user to this client")]
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();

        [Description("The userId to which th clientId is linked to")]
        [ForeignKey(nameof(Owner))]
        public int OwnerUserId { get; set; }
        public virtual User? Owner { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
