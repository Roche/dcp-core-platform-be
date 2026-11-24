namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents the status of an API gateway service.
    /// </summary>
    public enum ApiGatewayStatus : byte
    {
        /// <summary>
        /// The API gateway is active and operational.
        /// </summary>
        Active = 1,

        /// <summary>
        /// The API gateway is disabled and not currently in use.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// The API gateway is not available, e.g., due to downtime or misconfiguration.
        /// </summary>
        NotAvailable = 3,
    }

}
