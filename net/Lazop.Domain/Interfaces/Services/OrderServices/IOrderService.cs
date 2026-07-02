using Lazop.Domain.RequestModels.OrderRequestModels;
using Lazop.Domain.ViewModels.OrderViewModels;

namespace Lazop.Domain.Interfaces.Services.OrderServices
{
    public interface IOrderService
    {
        OrderResponseViewModel GetOrders(GetOrdersRequestModel param);
        OrderResponseViewModel GetOrderDetail(GetOrderDetailRequestModel param);
        OrderResponseViewModel GetMultipleOrderItems(GetMultipleOrderItemsRequestModel param);
    }
}
