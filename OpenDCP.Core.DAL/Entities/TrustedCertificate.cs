using OpenDCP.Framework.Security.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenDCP.Core.DAL.Entities
{
    public class TrustedCertificate
    {
        [Key]
        [Description("Unique identifier of the trusted certificate record.")]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Description("Friendly name used to identify the trusted certificate.")]
        public required string Name { get; set; }

        [Required]
        [Description("Thumbprint of the certificate used to validate incoming requests.")]
        public required string CertificateThumbprint { get; set; }

        [Description("Indicates whether the trusted certificate is currently enabled and allowed to authenticate.")]
        public bool Active { get; set; }

        [Description("Collection of permissions granted to identities authenticated using this trusted certificate.")]
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    }
}
