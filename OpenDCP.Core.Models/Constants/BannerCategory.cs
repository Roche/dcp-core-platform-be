namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Represents the category of a banner, used for display and styling purposes.
    /// </summary>
    public enum BannerCategory : byte
    {
        /// <summary>
        /// Informational banners, typically used to display general information to users.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Downtime banners, used to inform users about planned or unplanned system outages.
        /// </summary>
        Downtime = 1,

        /// <summary>
        /// Error banners, used to display critical issues or error messages to users.
        /// </summary>
        Error = 2
    }
}
