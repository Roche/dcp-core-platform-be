using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.Models.Models.Messages
{
    /// <summary>
    /// Represents user-submitted feedback for a specific system module,
    /// including subject, message content, and delivery preferences.
    /// </summary>
    public class UserFeedbackDTO
    {
        /// <summary>
        /// Gets or sets the name of the system module to which the feedback relates.
        /// </summary>
        [StringLength(250)]
        public required string SystemModule { get; set; }

        /// <summary>
        /// Gets or sets the subject or short summary of the feedback.
        /// </summary>
        [StringLength(256)]
        public required string Subject { get; set; }

        /// <summary>
        /// Gets or sets the detailed feedback message provided by the user.
        /// </summary>
        [StringLength(5000)]
        public required string Body { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a copy of the feedback
        /// should be sent to the submitting user.
        /// </summary>
        public bool SendCopyToMyself { get; set; }
    }
}
