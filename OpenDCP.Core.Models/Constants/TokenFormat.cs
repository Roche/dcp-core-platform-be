namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Specifies the format version used for issuing and validating tokens.
    /// </summary>
    public enum TokenFormat : byte
    {
        /// <summary>
        /// Legacy token format (version 1).
        /// </summary>
        /// <remarks>
        /// This format is obsolete and should no longer be used for new tokens.
        /// It is retained only for backward compatibility with older clients.
        /// </remarks>
        [Obsolete("Legacy to be removed")]
        V1 = 1,

        /// <summary>
        /// Current token format (version 2).
        /// </summary>
        /// <remarks>
        /// This format introduces the latest structure and security improvements
        /// and should be used for all new token generation and validation.
        /// </remarks>
        V2 = 2,
    }
}
