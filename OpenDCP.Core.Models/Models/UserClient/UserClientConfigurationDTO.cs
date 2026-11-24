using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Security.Models;
using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.Models.Models.UserClient
{
    /// <summary>
    /// Represents the configuration settings for a user-specific client,
    /// including authentication credentials, metadata, and assigned permissions.
    /// </summary>
    public class UserClientConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the client configuration.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the public client identifier used for authentication.
        /// </summary>
        [Required]
        public required string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the secret associated with the client identifier.
        /// </summary>
        [Required]
        public required string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the display name of the client.
        /// </summary>
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets an optional description providing additional details about the client.
        /// </summary>
        [StringLength(400)]
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the current status of the client.
        /// </summary>
        public UserClientStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the list of permissions assigned to the client.
        /// </summary>
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();

        /// <summary>
        /// Gets or sets the user name associated with this client configuration.
        /// </summary>
        [StringLength(150)]
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the user name associated with this client configuration.
        /// </summary>
        [StringLength(150)]
        public string? OwnedBy { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who owns this client configuration.
        /// </summary>
        public int OwnerUserId { get; set; }

        /// <summary>
        /// Gets or sets the Timestmap when the record has been modifed last
        /// </summary>
        public DateTime LastModifiedAt { get; set; }
    }
}
