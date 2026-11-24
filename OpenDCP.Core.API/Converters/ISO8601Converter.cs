using OpenDCP.Core.Core.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.API.Converters
{
    /// <summary>
    /// Converts <see cref="DateTime"/> values to and from ISO 8601 string representations for JSON serialization.
    /// </summary>
    /// <remarks>
    /// - When writing JSON, the <see cref="DateTime"/> is converted to UTC, rounded to milliseconds, 
    ///   and formatted as <c>yyyy-MM-ddTHH:mm:ss.fffZ</c>.  
    /// - When reading JSON, the ISO 8601 string is parsed into a <see cref="DateTime"/> object.
    /// </remarks>
    public class Iso8601Converter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Reads a <see cref="DateTime"/> value from JSON.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
        /// <param name="typeToConvert">The type of the object to convert (should be <see cref="DateTime"/>).</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>The parsed <see cref="DateTime"/> value.</returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString()!.ParseISO8601();
        }

        /// <summary>
        /// Writes a <see cref="DateTime"/> value to JSON in ISO 8601 format (UTC, rounded to milliseconds).
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
        /// <param name="value">The <see cref="DateTime"/> value to write.</param>
        /// <param name="options">Serialization options.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().RoundToMillisecondsUtc().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}
