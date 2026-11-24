using OpenDCP.Framework.Configuration.Model.Entities.Site;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.DataHandling
{
    public class LegacyRequestSensorData
    {
        public required List<string> WebIds { get; set; }
        public bool Refresh { get; set; }
        public List<string> DeviceWebIds { get; set; }
        /// <summary>
        /// Sensors - 1 Equation -2
        /// </summary>
        public byte SensorCalculationType { get; set; }
        public List<string>? Expressions { get; set; }
        public Dictionary<string, string> SensorsWebIdsPerDeviceWebid { get; set; }
        public LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string Interval { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618