using Newtonsoft.Json.Linq;

namespace OpenDCP.Core.Models.Legacy.Dashboard
{
    [Obsolete]
    public class DashboardLayoutDetailsDTO
    {
        public Guid ID { get; set; }

        public JObject GridLayout { get; set; }
    }
}
