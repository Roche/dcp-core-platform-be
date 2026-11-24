using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

#pragma warning disable CS1591
#pragma warning disable CS8618

namespace OpenDCP.Core.Models.Legacy.Site
{
    public class LegacyModuleDTO
    {

        public int ID { get; set; }
        [StringLength(250)]
        public string ModuleName { get; set; }
        public JsonObject RConnection { get; set; }
        public int SiteID { get; set; }
        public bool IsEnable { get; set; }
        public bool IsValidated { get; set; }
        public List<LegacyDataTypeDTO> DataType { get; set; }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS8618
