using System.Text.Json.Nodes;

namespace OpenDCP.Core.Models.Legacy.Site
{
    /// <summary>
    /// Represents a shareable link item, including its unique identifier, title, owner, and associated aliases.
    /// </summary>
    /// #pragma warning disable CS1591
#pragma warning disable CS1591
#pragma warning disable CS8618
    public class LegacyShareLinkListItemDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of the share link.
        /// </summary>
        public Guid Guid { get; set; }
        public required DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public required JsonObject Link { get; set; }
        /// <summary>
        /// Gets or sets the title of the share link.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who owns the share link.
        /// </summary>
        /// <remarks>
        /// Nullable if the owner is not specified.
        /// </remarks>
        public int UserIDCreated { get; set; }
        public int? UserIDUpdated { get; set; }
        /// <summary>
        /// Gets or sets the list of aliases associated with the share link.
        /// </summary>
        /// <remarks>
        /// Initialized to an empty list by default to avoid null references.
        /// </remarks>
        public List<LegacySharedLinkAliasDTO> ShareLinkAliasDetails { get; set; } = new List<LegacySharedLinkAliasDTO>();
    }
#pragma warning restore CS1591
#pragma warning restore CS8618

}
