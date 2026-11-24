using OpenDCP.Core.Models.Models.EventSubscription;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDCP.Core.DAL.Entities
{
    public class InProgressEvent
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Description("BatchId of the currently active eventframe")]
        public required string EventId { get; set; }
        [StringLength(50)]
        [Description("Start time of the active eventframe")]
        public DateTime EventStartTime { get; set; }
        [Description("Eventframe attributes, as JSON object")]
        public List<JEventValue> JEventValues { get; set; } = new List<JEventValue>();
        [StringLength(50)]
        [Description("UniqueId of the active eventframe returned by the datasource")]
        public string EventDisplayId { get; set; } = string.Empty;
        [Description("Relation of the eventframe to the asset/device on the datasource")]
        public required string DeviceWebId { get; set; }
        [Description("Nbr of attemps made by the service to fix/update bad values")]
        public int UpdateAttempts { get; set; }
        [ForeignKey("Site")]
        [Description("Relation to the site, to enable site depended computation offloading")]
        public int DatasourceId { get; set; }
        public virtual DataSource? Datasource { get; set; }
    }
}
