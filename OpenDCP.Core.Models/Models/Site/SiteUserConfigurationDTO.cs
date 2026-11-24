using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Core.Models.Models.Module;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the user-specific configuration of a site, including its metadata,
    /// enabled state, associated modules and datasources, and an optional reason for changes.
    /// </summary>
    public class SiteUserConfigurationDTO
    {
        /// <summary>
        /// Gets or sets the full name of the site.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the short name or abbreviation of the site.
        /// </summary>
        public required string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the type of the site.
        /// </summary>
        /// <remarks>
        /// Uses the <see cref="SiteType"/> enumeration to classify the site.
        /// </remarks>
        public SiteType Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the site is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the time zone of the site, typically in IANA or Windows format (e.g., "Europe/Berlin").
        /// </summary>
        public required string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the list of module configurations specific to the user for this site.
        /// </summary>
        public List<ModuleUserConfigurationDTO> Modules { get; set; } = new List<ModuleUserConfigurationDTO>();

        /// <summary>
        /// Gets or sets the list of datasource configurations specific to the user for this site.
        /// </summary>
        public List<DatasourceUserConfigurationDTO> Datasources { get; set; } = new List<DatasourceUserConfigurationDTO>();
        /// <summary>
        /// Gets or sets the list of connection bridges configurations associated with the site.
        /// </summary>
        public List<ConnectionBridgeUserConfigurationForm> ConnectionBridges { get; set; } = new List<ConnectionBridgeUserConfigurationForm>();

        /// <summary>
        /// Gets or sets the reason for any changes made to the site configuration.
        /// </summary>
        public required string ChangeReason { get; set; }
    }

}
