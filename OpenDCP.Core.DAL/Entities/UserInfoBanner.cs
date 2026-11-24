using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.DAL.Entities
{
    public class UserInfoBanner
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("The name used by the admins for easy identification")]
        public required string Name { get; set; }
        /// <summary>
        /// UserInfoBannerCategory.cs
        /// </summary>
        [Description("The catregory (Downtime/etc. used for the styling of the banner)")]
        public BannerCategory Category { get; set; }
        [Description("The description/info text shown to the users")]
        public required string Description { get; set; }
        public bool IsActive { get; set; }
        [Description("The start of the period where this banner should be shown (UTC)")]
        public DateTime StartTime { get; set; }
        [Description("The end of the period where this banner should be shown (UTC)")]
        public DateTime EndTime { get; set; }
        [Description("If set the banner will be only shown to users in the context of this production site")]
        public int? SiteId { get; set; }
        [Description("If set the banner will be only shown to users in the context of this module")]
        public string? ModuleName { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }
    }
}
