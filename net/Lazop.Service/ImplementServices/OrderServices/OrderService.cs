using System;
using Lazop.Domain.Interfaces;
using Lazop.Domain.Interfaces.Services.OrderServices;
using Lazop.Domain.RequestModels;
using Lazop.Domain.RequestModels.OrderRequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Domain.ViewModels.OrderViewModels;

namespace Lazop.Service.ImplementServices.OrderServices
{
    /// <summary>
    /// Service wrapper for Lazada Order APIs (/orders/*, /order/*).
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly ILazopClient _client;

        public OrderService(ILazopClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Retrieves a list of orders based on filters.
        /// API path: /orders/get
        /// </summary>
        public OrderResponseViewModel GetOrders(GetOrdersRequestModel param)
        {
            LazopRequest request = new LazopRequest("/orders/get");
            request.SetHttpMethod("GET");
            request.AddApiParameter("created_after", param.CreatedAfter.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            request.AddApiParameter("limit", param.Limit.ToString());
            
            var response = _client.Execute(request, param.AccessToken);
            return new OrderResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };
        }

        /// <summary>
        /// Retrieves detailed information of a single order item.
        /// API path: /order/get
        /// </summary>
        public OrderResponseViewModel GetOrderDetail(GetOrderDetailRequestModel param)
        {
            LazopRequest request = new LazopRequest("/order/get");
            request.SetHttpMethod("GET");
            request.AddApiParameter("order_id", param.OrderId);
            
            var response = _client.Execute(request, param.AccessToken);
            return new OrderResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };
        }

        /// <summary>
        /// Retrieves items for multiple orders.
        /// API path: /orders/items/get
        /// </summary>
        public OrderResponseViewModel GetMultipleOrderItems(GetMultipleOrderItemsRequestModel param)
        {
            LazopRequest request = new LazopRequest("/orders/items/get");
            request.SetHttpMethod("GET");
            
            string orderIdsStr = $"[{string.Join(",", param.OrderIds)}]";
            request.AddApiParameter("order_ids", orderIdsStr);
            
            var response = _client.Execute(request, param.AccessToken);
            return new OrderResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };
        }
    }
}
