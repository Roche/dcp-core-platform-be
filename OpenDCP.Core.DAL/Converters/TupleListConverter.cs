using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.DAL.Converters
{
    public class TupleListConverter : JsonConverter<List<(string sKey, string tKey)>>
    {
        public override List<(string sKey, string tKey)>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var array = JsonSerializer.Deserialize<List<string[]>>(ref reader, options);
            if (array == null) return new List<(string sKey, string tKey)>();
            return array.Select(a => (a.ElementAtOrDefault(0) ?? "", a.ElementAtOrDefault(1) ?? "")).ToList();
        }

        public override void Write(Utf8JsonWriter writer, List<(string sKey, string tKey)> value, JsonSerializerOptions options)
        {
            var array = value.Select(t => new[] { t.sKey, t.tKey }).ToList();
            JsonSerializer.Serialize(writer, array, options);
        }
    }
}
