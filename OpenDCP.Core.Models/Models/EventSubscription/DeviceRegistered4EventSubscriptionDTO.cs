namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents a device registration used for event subscription purposes.
    /// </summary>
    public class DeviceRegistered4EventSubscriptionDTO
    {
        /// <summary>
        /// The unique web identifier of the device.
        /// </summary>
        /// <example>"device-12345"</example>
        public required string DeviceWebId { get; set; }

        /// <summary>
        /// The list of site IDs associated with the device for event subscription.
        /// </summary>
        /// <example>[1, 2, 3]</example>
        public List<int> SiteIds { get; set; } = new List<int>();

        /// <summary>
        /// The name of the module for which the device is registered.
        /// </summary>
        /// <example>"mvda"</example>
        public required string ModuleName { get; set; }
    }

}
