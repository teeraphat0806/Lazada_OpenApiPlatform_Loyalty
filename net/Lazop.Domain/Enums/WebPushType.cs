using System.Text.Json.Serialization;
using Lazop.Domain.Utils;

namespace Lazop.Domain.Enums
{
    [JsonConverter(typeof(WebPushTypeConverter))]
    public enum WebPushType
    {
        TradeOrder = 0,
        ProductQuality = 1,
        ImSendMessage = 2,
        ProductUpdate = 3,
        ShallowStock = 6,
        StockVideoState = 7,
        AuthorizationTokenExpiration = 8,
        ReverseOrder = 10,
        Promotion = 11,
        ProductCategoryUpdate = 12,
        SellerStatusUpdate = 13,
        FulfillmentOrderUpdate = 14,
        ImSessionUpdate = 19,
        ProductReview = 21,
        JitPOStatus = 35,
        JitPickupOrder = 46
    }
}
