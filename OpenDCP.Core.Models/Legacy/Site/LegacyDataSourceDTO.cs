using System.Text.Json.Nodes;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacyDataSourceDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// OSISoftPI = 1, GCP = 2, SynTQ = 3, SQL = 4
        /// </summary>
        public byte Type { get; set; }
        public JsonObject ConnectionString { get; set; }
        public int SiteID { get; set; }
        public bool IsValidated { get; set; }
        public List<byte> SupportedDataTypes { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618