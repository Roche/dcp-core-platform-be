namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Provides configuration values related to Entity Framework behavior.
    /// </summary>
    /// <remarks>
    /// This class contains constants used to control timing and performance
    /// characteristics of Entity Framework operations across the application.
    /// </remarks>
    public static class EntityFrameworkConfiguration
    {
        /// <summary>
        /// Specifies the delay, in milliseconds, applied after calling
        /// <c>SaveChanges</c> or <c>SaveChangesAsync</c>.
        /// </summary>
        /// <remarks>
        /// This delay is needed to keep audit trail states correctly in case of delete
        /// saving the user performing the delete
        /// </remarks>
        public const int SaveChangesDelayInMilliseconds = 800;
    }

}
