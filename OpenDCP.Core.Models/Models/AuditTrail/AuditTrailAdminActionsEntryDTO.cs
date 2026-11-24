using System.Text.Json.Nodes;

namespace OpenDCP.Core.Models.Models.AuditTrail
{
    /// <summary>
    /// Represents an entry in the audit trail for administrative actions, including user, action details, and context.
    /// </summary>
    public class AuditTrailAdminActionsEntryDTO
    {
        /// <summary>
        /// Gets or sets the identifier of the user who performed the action.
        /// May be <c>null</c> if the user is not specified.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was performed.
        /// May be <c>null</c> if unknown.
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional configuration details for the action in JSON format.
        /// </summary>
        public required JsonObject ActionConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the reason for performing the administrative action.
        /// </summary>
        public required string Reason { get; set; }

        /// <summary>
        /// Gets or sets the module in which the administrative action was performed.
        /// </summary>
        public required string Module { get; set; }

        /// <summary>
        /// Gets or sets the exception message, if any occurred during the action.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action is subject to GxP (Good Practice) compliance.
        /// </summary>
        public bool IsGxp { get; set; }
    }

}
