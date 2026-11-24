using System.Text.Json.Nodes;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacySiteConfigurationDTO
    {
        public int ID { get; set; }

        public required string Name { get; set; }

        public JsonObject RConnection { get; set; }

        public byte ConnectionType { get; set; }

        public JsonObject? ConnectionString { get; set; }

        public bool IsModuleValidated { get; set; }

        public bool IsDataSourceValidated { get; set; }

        public int DataSourceId { get; set; }

        public required string ShortName { get; set; }

        public required string TimeZone { get; set; }

        public bool IsEnable { get; set; }
        public bool IsModuleEnabled { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618
