using System.Collections.Generic;

namespace Lazop.Domain.RequestModels.OrderRequestModels
{
    public class GetMultipleOrderItemsRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public List<long> OrderIds { get; set; } = new List<long>();
    }
}
