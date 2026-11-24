namespace OpenDCP.Core.Models.Models.EventSubscription
{
    /// <summary>
    /// Represents the parameters used to query event subscriptions,
    /// including device identifiers, site IDs, and event frame identifiers.
    /// </summary>
    public class EventSubscriptionQuery
    {
        /// <summary>
        /// Gets or sets the collection of device Web IDs for which event subscriptions are queried.
        /// </summary>
        public List<string> DeviceWebId { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the collection of site IDs to filter the event subscriptions.
        /// </summary>
        public List<int> SiteId { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the collection of event frame IDs to filter the subscriptions.
        /// </summary>
        public List<string> EventframeId { get; set; } = new List<string>();
    }

}
