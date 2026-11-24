namespace OpenDCP.Core.Models.Models.User
{
    /// <summary>
    /// Represents user-specific settings related to date and time formatting.
    /// </summary>
    public class UserSettingsDTO
    {
        /// <summary>
        /// Gets or sets the preferred time format for the user
        /// (e.g., 24-hour or 12-hour format).
        /// </summary>
        public string TimeFormat { get; set; } = "HH:mm:ss";

        /// <summary>
        /// Gets or sets the preferred date format for the user
        /// (e.g., ISO, regional, or custom format).
        /// </summary>
        public string DateFormat { get; set; } = "dd MMM yyyy";
    }
}
