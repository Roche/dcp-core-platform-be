using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.Security.Models;

namespace OpenDCP.Core.Models.Models.Authentication
{
    /// <summary>
    /// Represents the definition of a token, including its type, associated client secret, and permissions.
    /// </summary>
    public class TokenDefinitionDTO
    {
        /// <summary>
        /// Gets or sets the type of the persisted grant (token type).
        /// </summary>
        public PersistedGrantsType Type { get; set; }

        /// <summary>
        /// Gets or sets the client secret associated with the token.
        /// Defaults to an empty string if not provided.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of permissions associated with the token.
        /// </summary>
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }

}
