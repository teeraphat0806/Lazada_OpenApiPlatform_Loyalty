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
                if (val == 0) return WebPushType.TradeOrder;
                if (val == 10) return WebPushType.ReverseOrder;
                
                throw new JsonException($"Unknown WebPushType integer value: {val}");
            }
            
            if (reader.TokenType == JsonTokenType.String)
            {
                string? strVal = reader.GetString();
                if (string.Equals(strVal, "TradeOrder", StringComparison.OrdinalIgnoreCase) || strVal == "0")
                {
                    return WebPushType.TradeOrder;
                }
                if (string.Equals(strVal, "ReverseOrder", StringComparison.OrdinalIgnoreCase) || strVal == "10")
                {
                    return WebPushType.ReverseOrder;
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
