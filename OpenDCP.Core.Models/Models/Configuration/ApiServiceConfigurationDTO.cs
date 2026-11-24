namespace OpenDCP.Core.Models.Models.Configuration
{
    /// <summary>
    /// Represents the configuration of an API service, including its name and current status.
    /// </summary>
    public class ApiServiceConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the name of the API service.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string Name { get; set; }
        /// <summary>
        /// Gets or sets the dispaly name of the API service.
        /// </summary>
        /// <remarks>
        /// This property is required.
        /// </remarks>
        public required string Label { get; set; }

        /// <summary>
        /// Gets or sets the current status of the API gateway.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="ApiGatewayStatus"/> enumeration to indicate the service state.
        /// </remarks>
        public ApiGatewayStatus Status { get; set; }
    }

}
