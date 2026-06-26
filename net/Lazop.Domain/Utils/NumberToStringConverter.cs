using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lazop.Domain.Utils
{
    public class NumberToStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                using (var jsonDoc = JsonDocument.ParseValue(ref reader))
                {
                    return jsonDoc.RootElement.GetRawText();
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                return jsonDoc.RootElement.GetRawText();
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
