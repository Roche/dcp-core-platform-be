using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;

namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents a subscribed event, including its identifiers, timing, related device,
    /// connection information, values, and subscribers.
    /// </summary>
    public class SubscribedEventDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the event.
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
        public string EventDisplayId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the web identifier of the associated device.
        /// </summary>
        public required string DeviceWebId { get; set; }

        /// <summary>
        /// Gets or sets the connection identifier associated with the event.
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the list of values associated with this event frame.
        /// </summary>
        public List<EventFrameValueDTO> Values { get; set; } = new List<EventFrameValueDTO>();

        /// <summary>
        /// Gets or sets the list of subscribers for this event.
        /// Each subscriber is represented as a tuple containing the module name and the site ID.
        /// </summary>
        public List<(string moduleName, int SiteId)> EventSubscribers { get; set; } = new List<(string moduleName, int SiteId)>();

    }
}