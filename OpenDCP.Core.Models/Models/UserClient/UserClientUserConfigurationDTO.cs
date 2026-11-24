using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Security.Models;
using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.Models.Models.UserClient
{
    /// <summary>
    /// Represents a user configuration for a client, including metadata, status, and associated permissions.
    /// </summary>
    public class UserClientUserConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user configuration.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets a brief description of the user configuration.
        /// Maximum length is 400 characters.
        /// </summary>
        [StringLength(400)]
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the current status of the user configuration.
        /// </summary>
        public UserClientStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the collection of permissions associated with this user configuration.
        /// </summary>
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
