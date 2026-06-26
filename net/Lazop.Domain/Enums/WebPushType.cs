using System.Text.Json.Serialization;
using Lazop.Domain.Utils;

namespace Lazop.Domain.Enums
{
    [JsonConverter(typeof(WebPushTypeConverter))]
    public enum WebPushType
    {
        TradeOrder = 0,
        ReverseOrder = 10
    }
}
