namespace Lazop.Domain.RequestModels.OrderRequestModels
{
    public class GetOrderDetailRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
    }
}
