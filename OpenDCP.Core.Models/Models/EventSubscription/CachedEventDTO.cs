using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;

namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents a cached event with associated metadata, timing information,
    /// and captured event frame values.
    /// </summary>
    public class CachedEventDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the cached event record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the site associated with the event.
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the connection through which the event was received.
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the event from the source system.
        /// </summary>
        public required string EventId { get; set; }

        /// <summary>
        /// Gets or sets the start time of the event.
        /// </summary>
        public DateTime EventStartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the event.
        /// </summary>
        public DateTime EventEndTime { get; set; }

        /// <summary>
        /// Gets or sets the display identifier of the event.
        /// </summary>
        public required string EventDisplayId { get; set; }

        /// <summary>
        /// Gets or sets the web identifier of the device associated with the event.
        /// </summary>
        public required string DeviceWebId { get; set; }

        /// <summary>
        /// Gets or sets the collection of values captured for the event frame.
        /// </summary>
        public List<EventFrameValueDTO> Values { get; set; } = new List<EventFrameValueDTO>();

        /// <summary>
        /// Gets or sets the number of attempts made by the service to fix/update bad values.
        /// </summary>
        public int UpdateAttempts { get; set; }
    }
}
