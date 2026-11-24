namespace OpenDCP.Core.Models.Models.User
{
    /// <summary>
    /// Data transfer object defining query criteria for filtering users.
    /// </summary>
    /// <remarks>
    /// All populated properties are combined using logical <b>AND</b>.
    /// Multiple values within the same property list are combined using logical <b>OR</b>.
    ///
    /// Example:
    /// <code>
    /// UserId = [1, 2] AND
    /// Email = ["a@example.com", "b@example.com"]
    /// </code>
    /// Results in:
    /// <c>(UserId = 1 OR UserId = 2) AND (Email = "a@example.com" OR Email = "b@example.com")</c>
    /// </remarks>
    public class UserQueryDTO
    {
        /// <summary>
        /// Gets or sets the user IDs to query.
        /// </summary>
        /// <remarks>
        /// Multiple IDs are combined using logical <b>OR</b>.
        /// </remarks>
        public List<int> UserId { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the site IDs associated with the users.
        /// </summary>
        /// <remarks>
        /// Multiple site IDs are combined using logical <b>OR</b>.
        /// </remarks>
        public List<int> SiteId { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the email addresses to query.
        /// </summary>
        /// <remarks>
        /// Multiple email addresses are combined using logical <b>OR</b>.
        /// </remarks>
        public List<string> Email { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the Unix identifiers to query.
        /// </summary>
        /// <remarks>
        /// Multiple Unix identifiers are combined using logical <b>OR</b>.
        /// </remarks>
        public List<string> UnixId { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the permissions used to filter users.
        /// </summary>
        /// <remarks>
        /// Multiple permissions are combined using logical <b>OR</b>.
        /// </remarks>
        public List<string> Permission { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the module used to filter users.
        /// </summary>
        /// <remarks>
        /// Multiple permissions are combined using logical <b>OR</b>.
        /// </remarks>
        public List<string> Module { get; set; } = new List<string>();
    }
}
