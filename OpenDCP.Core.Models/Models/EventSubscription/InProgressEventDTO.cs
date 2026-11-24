using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;

namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents an event that is currently in progress, including its timing, associated values, and device information.
    /// </summary>
    public class InProgressEventDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the in-progress event.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the external or business identifier of the event.
        /// </summary>
        public required string EventId { get; set; }

        /// <summary>
        /// Gets or sets the start time of the event.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the collection of event frame values associated with this event.
        /// </summary>
        public List<EventFrameValueDTO> JEventValues { get; set; } = new List<EventFrameValueDTO>();

        /// <summary>
        /// Gets or sets the Web ID of the device associated with this event.
        /// </summary>
        public required string DeviceWebId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the site where the event is taking place.
        /// </summary>
        public int SiteId { get; set; }
    }

}
