using System;
using Microsoft.Extensions.Logging;
using Lazop.Domain.Interfaces.Services.WebhookServices;
using Lazop.Domain.Models;
using Lazop.Domain.RequestModels.WebhookRequestModels;

namespace Lazop.Service.ImplementServices.WebhookServices
{
    public class LazadaWebhookService : ILazadaWebhookService
    {
        private readonly ILogger<LazadaWebhookService> _logger;

        public LazadaWebhookService(ILogger<LazadaWebhookService> logger)
        {
            _logger = logger;
        }

        public void CreateOrUpdateOrder(LazadaWebhookRequest data)
        {
            var sellerId = data.SellerId;
            var orderId = data.Data?.TradeOrderId;
            var reverseOrderId = data.Data?.ReverseOrderId;
            var status = data.Data?.OrderStatus;

            if (!string.IsNullOrWhiteSpace(orderId) && !string.IsNullOrWhiteSpace(status))
            {
                var order = InMemoryStorage.LazadaOrders.GetOrAdd(orderId, id => new LazadaOrder { Id = id });
                order.SellerId = sellerId;
                order.Status = status;
                
                _logger.LogInformation("Lazada Webhook: Order {OrderId} updated to status {Status}", orderId, status);
            }

            if (!string.IsNullOrWhiteSpace(reverseOrderId))
            {
                var reverseOrder = InMemoryStorage.LazadaReverseOrders.GetOrAdd(reverseOrderId, id => new LazadaReverseOrder { Id = id });
                reverseOrder.OrderId = orderId;
                
                _logger.LogInformation("Lazada Webhook: Reverse Order {ReverseOrderId} linked to Order {OrderId}", reverseOrderId, orderId);
            }
        }

        public void CreateOrUpdateReverseOrder(LazadaWebhookRequest data)
        {
            var orderId = data.Data?.TradeOrderId;
            var reverseOrderId = data.Data?.ReverseOrderId;
            var sellerId = data.SellerId;
            var buyerId = data.Data?.BuyerId;
            var status = data.Data?.ReverseStatus;

            if (!string.IsNullOrWhiteSpace(reverseOrderId) && !string.IsNullOrWhiteSpace(status))
            {
                var reverseOrder = InMemoryStorage.LazadaReverseOrders.GetOrAdd(reverseOrderId, id => new LazadaReverseOrder { Id = id });
                reverseOrder.OrderId = orderId;
                reverseOrder.SellerId = sellerId;
                reverseOrder.BuyerId = buyerId;
                reverseOrder.Status = status;

                _logger.LogInformation("Lazada Webhook: Reverse Order {ReverseOrderId} updated to status {Status}", reverseOrderId, status);
            }
        }
    }
}
