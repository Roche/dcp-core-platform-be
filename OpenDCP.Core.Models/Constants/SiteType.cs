namespace OpenDCP.Core.Models.Constants
{
    /// <summary>
    /// Represents different types of sites.
    /// </summary>
    public enum SiteType : byte
    {
        /// <summary>
        /// Represents a global site.
        /// </summary>
        Global = 0,

        /// <summary>
        /// Represents a drug substance site.
        /// </summary>
        DrugSubstance = 1,

        /// <summary>
        /// Represents a drug product site.
        /// </summary>
        DrugProduct = 2,

        /// <summary>
        /// Represents a development site.
        /// </summary>
        Development = 3,

        /// <summary>
        /// Represents a small molecules site.
        /// </summary>
        SmallMolecules = 4,

        /// <summary>
        /// Represents a diagnostic site.
        /// </summary>
        Diagnostic = 5,
    }
}
