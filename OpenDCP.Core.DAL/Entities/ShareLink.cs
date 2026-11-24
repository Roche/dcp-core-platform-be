using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.DAL.Entities
{
    public class ShareLink
    {
        public ShareLink()
        {
            ShareLinkAliases = new HashSet<ShareLinkAlias>();
        }
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        public required JsonObject JLink { get; set; }
        public Guid LinkGuid { get; set; }
        [StringLength(250)]
        public required string Title { get; set; }
        public bool IsDeleted { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        [ForeignKey("Site")]
        public int? SiteId { get; set; }
        public Site? Site { get; set; }
        public virtual ICollection<ShareLinkAlias> ShareLinkAliases { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
