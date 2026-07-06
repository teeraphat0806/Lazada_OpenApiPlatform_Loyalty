using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lazop.Domain.Enums;

namespace Lazop.Domain.Utils
{
    public class WebPushTypeConverter : JsonConverter<WebPushType>
    {
        public override WebPushType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int val = reader.GetInt32();
                if (Enum.IsDefined(typeof(WebPushType), val))
                {
                    return (WebPushType)val;
                }
                
                throw new JsonException($"Unknown WebPushType integer value: {val}");
            }
            
            if (reader.TokenType == JsonTokenType.String)
            {
                string? strVal = reader.GetString();
                if (Enum.TryParse<WebPushType>(strVal, true, out var result))
                {
                    return result;
                }
                
                throw new JsonException($"Unknown WebPushType string value: {strVal}");
            }
            
            throw new JsonException($"Unexpected token type {reader.TokenType} for WebPushType.");
        }

        public override void Write(Utf8JsonWriter writer, WebPushType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
