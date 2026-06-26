using System;

namespace Lazop.Domain.RequestModels.OrderRequestModels
{
    public class GetOrdersRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime CreatedAfter { get; set; }
        public int Limit { get; set; } = 20;
    }
}
