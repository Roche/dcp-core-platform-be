#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacyDataTypeDTO
    {
        public int DataTypeId { get; set; }
        /// <summary>
        /// EquipmentData = 1, ExperimentData = 2, ProcessMonitoringData = 3
        /// </summary>
        public byte DataType { get; set; }
        public int DataSourceId { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618