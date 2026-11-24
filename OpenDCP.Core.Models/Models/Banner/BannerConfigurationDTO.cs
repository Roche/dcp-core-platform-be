using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.Banner
{
    /// <summary>
    /// Represents the configuration of a user information banner.
    /// </summary>
    /// <remarks>
    /// This DTO is used for creating, updating, and managing banners by
    /// administrative users. It includes visibility rules, scheduling,
    /// and display-related properties.
    /// </remarks>
    public class BannerConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the banner.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the administrative name of the banner.
        /// </summary>
        /// <remarks>
        /// This name is used by administrators to identify and manage
        /// the banner and is not displayed to end users.
        /// </remarks>
        /// <example>Sample Banner</example>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the category of the banner.
        /// </summary>
        /// <remarks>
        /// The category determines the visual styling of the banner
        /// when displayed to users.
        /// </remarks>
        /// <example>BannerCategory.Error</example>
        public BannerCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the textual content of the banner.
        /// </summary>
        /// <remarks>
        /// This text is displayed to users as the banner message.
        /// </remarks>
        /// <example>This is a sample banner.</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the banner is currently active.
        /// </summary>
        /// <remarks>
        /// Only active banners are considered for display, subject to
        /// scheduling and contextual constraints.
        /// </remarks>
        /// <example>true</example>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the banner becomes visible.
        /// </summary>
        /// <remarks>
        /// The banner will not be displayed before this time.
        /// </remarks>
        /// <example>2024-12-29T00:00:00Z</example>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the banner is no longer visible.
        /// </summary>
        /// <remarks>
        /// The banner will not be displayed after this time.
        /// </remarks>
        /// <example>2024-12-30T00:00:00Z</example>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the site identifier for which the banner is applicable.
        /// </summary>
        /// <remarks>
        /// If <c>null</c>, the banner is site-agnostic and applies to all sites.
        /// If set, the banner is only displayed when the user is in the context
        /// of the specified site.
        /// </remarks>
        /// <example>1</example>
        public int? SiteId { get; set; }

        /// <summary>
        /// Gets or sets the module name for which the banner is applicable.
        /// </summary>
        /// <remarks>
        /// If empty or <c>null</c>, the banner is module-agnostic and applies to all modules.
        /// If set, the banner is only displayed when the user is in the context
        /// of the specified module.
        /// </remarks>
        /// <example>mvda</example>
        public string? ModuleName { get; set; }
    }
}
