using OpenDCP.Framework.Configuration.Model.Entities.Site;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.DataHandling
{
    public class LegacyPlotDataWithBatchContextRequest
    {
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int Intervals { get; set; }
        /// <summary>
        /// Sensors web ids 
        /// </summary>
        public List<string> SensorsWebIds { get; set; }
        public bool Refresh { get; set; } // TODO if true invoke interpolatedAtTimes[Datetime.nowUtc]
        public List<string> DeviceWebIds { get; set; }
        /// <summary>
        /// Sensors - 1 Equation -2
        /// </summary>
        public byte SensorCalculationType { get; set; }
        public List<string>? Expressions { get; set; } //TODO "@web1@-@web2@"
        public Dictionary<string, string> SensorsWebIdsPerDeviceWebid { get; set; }
        public LegacySiteConfigurationDTO_v2 SiteConfiguration { get; set; }
        public bool SynchronizedTimeStamps { get; set; } // if true probably reuse recorded data sync
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618
