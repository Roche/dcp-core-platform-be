using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.DAL.Entities
{
    public class ShareLinkSystemGenerated
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("JSON object storing the page configuration to reload the page")]
        public required JsonObject JLink { get; set; }
        [Description("Unique GUID used in the URL for the look-up")]
        public Guid LinkGuid { get; set; }
        [Description("Flag if this link is soft deleted")]
        public bool IsDeleted { get; set; }
        [Description("Creation datetime in UTC")]
        public DateTime? DateCreated { get; set; }
        [ForeignKey("Site")]
        [Description("Relation to the site for which the data should be displayed")]
        public int? SiteId { get; set; }
        public virtual Site? Site { get; set; }
    }
}
