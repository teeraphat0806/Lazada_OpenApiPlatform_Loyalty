using System.Text.Json.Serialization;
using Lazop.Domain.Enums;
using Lazop.Domain.Utils;

namespace Lazop.Domain.RequestModels.WebhookRequestModels
{
    public class LazadaWebhookRequest
    {
        [JsonPropertyName("message_type")]
        public WebPushType? MessageType { get; set; }

        [JsonPropertyName("msg_type")]
        public WebPushType? MsgType { get; set; }

        [JsonPropertyName("seller_id")]
        [JsonConverter(typeof(NumberToStringConverter))]
        public string? SellerId { get; set; }

        [JsonPropertyName("data")]
        public LazadaWebhookData? Data { get; set; }
    }

    public class LazadaWebhookData
    {
        [JsonPropertyName("trade_order_id")]
        [JsonConverter(typeof(NumberToStringConverter))]
        public string? TradeOrderId { get; set; }

        [JsonPropertyName("reverse_order_id")]
        [JsonConverter(typeof(NumberToStringConverter))]
        public string? ReverseOrderId { get; set; }

        [JsonPropertyName("order_status")]
        public string? OrderStatus { get; set; }

        [JsonPropertyName("reverse_status")]
        public string? ReverseStatus { get; set; }

        [JsonPropertyName("buyer_id")]
        [JsonConverter(typeof(NumberToStringConverter))]
        public string? BuyerId { get; set; }
    }
}
