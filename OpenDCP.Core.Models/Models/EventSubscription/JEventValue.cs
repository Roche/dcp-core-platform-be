namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents a value from an event frame to be stored, including its status and metadata.
    /// </summary>
    public class JEventValue
    {
        /// <summary>
        /// The name of the value or the associated tag.
        /// </summary>
        /// <example>"Temperature"</example>
        public required string Name { get; set; }

        /// <summary>
        /// The actual value to be stored for the event frame.
        /// </summary>
        /// <example>"75.4"</example>
        public required string Value { get; set; }

        /// <summary>
        /// Indicates whether the value is considered valid or "good".
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>.
        /// </remarks>
        public bool Good { get; set; } = true;
    }
}
