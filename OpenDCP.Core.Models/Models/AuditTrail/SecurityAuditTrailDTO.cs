using OpenDCP.Core.Models.Constants;

namespace OpenDCP.Core.Models.Models.AuditTrail
{
    /// <summary>
    /// Represents a data transfer object (DTO) for recording security-related audit trail entries.
    /// </summary>
    public class SecurityAuditTrailDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier associated with the audited entity or operation.
        /// </summary>
        public required string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the type or category of the audited entity or event.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the security action that was performed.
        /// </summary>
        public SecurityAction Action { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the security action occurred.
        /// Defaults to the current system time.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
