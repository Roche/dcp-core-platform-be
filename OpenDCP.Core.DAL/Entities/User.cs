using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using OpenDCP.Framework.Security.Models;

namespace OpenDCP.Core.DAL.Entities
{
    public class User
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }

        [StringLength(150)]
        [Description("The unique identifier, as provided by the IdP, such as Active Directoy, PingId")]
        public required string UnixId { get; set; }
        
        [StringLength(250)]
        [Description("Displayname as provided by the auth provider")]
        public string DisplayName { get; set; } = string.Empty;

        [Description("Firstname as provided by the IdP")]
        [StringLength(250)]
        public string FirstName { get; set; } = string.Empty;

        [Description("Lastname as provided by the IdP")]
        [StringLength(250)]
        public string LastName { get; set; } = String.Empty;


        [Description("Email address as as provided by the IdP")]
        [StringLength(250)]
        public string Email { get; set; } = string.Empty;

        [Description("The current site, the user is logged to")]
        public int? CurrentSiteId { get; set; }

        [StringLength(50)]
        [Description("Timeformat 24:00 vs. 12:00 time representation")]
        public required string TimeFormat { get; set; }
        [StringLength(50)]
        [Description("Dateformat Europe vs US timeformat")]
        public required string DateFormat { get; set; }
        [Description("The permissions assigned to the user - updated on each login")]
        public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();

        [Description("The timestamp, the user has performed the last interactive login")]
        public DateTime LastLogin { get; set; }
    }
}
