using OpenDCP.Framework.Security.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.DAL.Converters
{
    public class PermissionsJsonConverter : JsonConverter<List<PermissionDTO>>
    {
        public override List<PermissionDTO> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new List<PermissionDTO>();
            }

            try
            {
                var result = JsonSerializer.Deserialize<List<PermissionDTO>>(ref reader, options);
                return result ?? new List<PermissionDTO>(); 
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing PermissionDTO list: {ex.Message}");
                return new List<PermissionDTO>();
            }
        }

        public override void Write(Utf8JsonWriter writer, List<PermissionDTO> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            try
            {
                JsonSerializer.Serialize(writer, value, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing PermissionDTO list: {ex.Message}");
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
        }
    }
}
