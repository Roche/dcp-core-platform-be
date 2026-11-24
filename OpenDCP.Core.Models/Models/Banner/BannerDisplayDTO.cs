using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.Banner
{
    /// <summary>
    /// Represents a banner displayed to the user.
    /// </summary>
    /// <remarks>
    /// This DTO is used to transfer banner information that is intended
    /// for display purposes only. It includes styling, scope, and visibility
    /// constraints such as site and module context.
    /// </remarks>
    public class BannerDisplayDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the banner.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the category of the banner.
        /// </summary>
        /// <remarks>
        /// The category is used to determine the visual styling
        /// of the banner when displayed to the user.
        /// </remarks>
        /// <example>BannerCategory.Error</example>
        public BannerCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the textual content of the banner.
        /// </summary>
        /// <remarks>
        /// This text is presented directly to the user as the banner message.
        /// </remarks>
        /// <example>This is a sample banner.</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the site identifier for which the banner is applicable.
        /// </summary>
        /// <remarks>
        /// If <c>null</c>, the banner is site-agnostic and displayed for all sites.
        /// If set, the banner is only displayed when the user is in the context
        /// of the specified site.
        /// </remarks>
        /// <example>1</example>
        public int? SiteId { get; set; }

        /// <summary>
        /// Gets or sets the module name for which the banner is applicable.
        /// </summary>
        /// <remarks>
        /// If <c>null</c>, the banner is module-agnostic and displayed for all modules.
        /// If set, the banner is only displayed when the user is in the context
        /// of the specified module.
        /// </remarks>
        /// <example>mvda</example>
        public string? ModuleName { get; set; }
    }
}
