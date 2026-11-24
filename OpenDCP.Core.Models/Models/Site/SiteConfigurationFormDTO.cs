using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Core.Models.Models.Module;

namespace OpenDCP.Core.Models.Models.Site
{
    /// <summary>
    /// Represents the configuration of a site, including its metadata, type, modules, and datasources.
    /// </summary>
    public class SiteConfigurationFormDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the site configuration.
        /// </summary>
        public int Id { get; set; }

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
        /// Gets or sets the time zone of the site, typically in IANA or Windows format (e.g., "Europe/Berlin").
        /// </summary>
        public required string TimeZone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the site is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the list of module configurations associated with the site.
        /// </summary>
        public List<ModuleConfigurationForm> Modules { get; set; } = new List<ModuleConfigurationForm>();

        /// <summary>
        /// Gets or sets the list of datasource configurations associated with the site.
        /// </summary>
        public List<DatasourceConfigurationForm> Datasources { get; set; } = new List<DatasourceConfigurationForm>();
        /// <summary>
        /// Gets or sets the list of connection bridges configurations associated with the site.
        /// </summary>
        public List<ConnectionBridgeConfigurationForm> ConnectionBridges { get; set; } = new List<ConnectionBridgeConfigurationForm>();
    }
}
