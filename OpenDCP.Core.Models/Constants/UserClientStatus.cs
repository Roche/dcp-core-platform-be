namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Represents the status of a user client.
    /// </summary>
    /// <remarks>
    /// This enum defines the possible statuses that a user client can have. The status indicates whether the client is active or inactive.
    /// </remarks>
    public enum UserClientStatus : byte
    {
        /// <summary>
        /// The user client is inactive or deactivated.
        /// </summary>
        Deactive = 0,

        /// <summary>
        /// The user client is active and operational.
        /// </summary>
        Active = 1,
    }
}
