using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace OpenDCP.Core.DAL.Entities
{
    public class Client
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("ClientId used for the DCP server to server communication")]
        public required string ClientId { get; set; }
        [Description("Secret used for the server to server authentication")]
        public required string ClientSecret { get; set; }
        [Required]
        [MaxLength(200)]
        [Description("Name of the client/secret pair for debugging")]
        public required string Name { get; set; }
        [MaxLength(400)]
        [Description("Description of the client")]
        public required string Description { get; set; }
        /// <summary>
        /// JavaScript = 0, NativeConfidential = 1
        /// </summary>
        public byte ApplicationType { get; set; }
        [Description("Can this client/secret pair be used to aquire a token")]
        public bool Active { get; set; }
        public int RefreshTokenLifeTime { get; set; }
        public required string AllowedOrigin { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
