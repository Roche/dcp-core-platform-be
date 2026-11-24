using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace OpenDCP.Core.DAL.Entities
{
    public class ShareLinkAlias
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [StringLength(250)]
        [Description("User defined name of the share link alias")]
        public required string Name { get; set; }
        [ForeignKey("SharedLink")]
        public int ShareLinkId { get; set; }
        public virtual ShareLink? ShareLink { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
