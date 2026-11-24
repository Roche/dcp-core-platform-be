using OpenDCP.Core.DAL.Converters;
using OpenDCP.Framework.AuditTrail.Attributes;
using OpenDCP.Framework.Connections.Manager.Constants;
using OpenDCP.Framework.Connections.Manager.Models.StrategyDefintions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace OpenDCP.Core.DAL.Entities
{
    [AuditTable(
        SiteJoin = "LEFT JOIN (SELECT DISTINCT \"Id\", \"SiteId\" from public.\"DataSource__from_to\"('-infinity','infinity')) ds on ds.\"Id\" = t.\"SConId\"",
        SiteIdColumn = "ds.\"SiteId\""
    )]
    public class ConnectionBridge
    {
        [Key]
        [Description("UniqueId")]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        [Description("A datatype enumeration value, see the enumartion for translation")]
        public ConnectionBridgeStrategyType Strategy { get; private set; }
        public byte SConDataType { get; set; }
        public byte TConDataType { get; set; }
        public int TConId { get; set; }
        [ForeignKey(nameof(TConId))]
        public required virtual DataSource TCon { get; set; }
        public int SConId { get; set; }

        [ForeignKey(nameof(SConId))]
        public required virtual DataSource SCon { get; set; }
        private string _definitionJSON { get; set; } = null!;
        public required string Reason { get; set; }
        public int? LastModifiedBy { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime SysStartTime { get; private set; }
        public DateTime SysEndTime { get; private set; }

        [NotMapped]
        public StrategyDefintion? Definition
        {
            get => DeserializeDefinition();
            set
            {
                Strategy = value?.Type ?? default;

                var options = new JsonSerializerOptions();
                options.Converters.Add(new TupleListConverter());

                _definitionJSON = value == null ? "{}" : JsonSerializer.Serialize(value, value.GetType(), options);
            }
        }

        private StrategyDefintion? DeserializeDefinition()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new TupleListConverter());

            return Strategy switch
            {
                ConnectionBridgeStrategyType.Path => JsonSerializer.Deserialize<PathMatchingStrategyDefition>(_definitionJSON),
                ConnectionBridgeStrategyType.IdLookUp => JsonSerializer.Deserialize<IdLookUpStrategyDefiniton>(_definitionJSON),
                ConnectionBridgeStrategyType.Attribute => JsonSerializer.Deserialize<AttributeMatchingStrategyDefiniton>(_definitionJSON, options),
                ConnectionBridgeStrategyType.QueryLookUp => JsonSerializer.Deserialize<QueryLookUpStrategyDefiniton>(_definitionJSON),
                _ => throw new NotSupportedException($"Unsupported type {Strategy}")
            };
        }
    }

}
