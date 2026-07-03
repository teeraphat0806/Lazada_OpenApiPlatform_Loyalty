using System;
using System.Text.Json;
using StackExchange.Redis;
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
        private readonly IDatabase? _cache;

        public OrderService(ILazopClient client, IConnectionMultiplexer? redisConnection = null)
        {
            _client = client;
            _cache = redisConnection?.GetDatabase();
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
            string cacheKey = $"lazada:cache:order:{param.OrderId}";
            if (_cache != null)
            {
                try
                {
                    var cachedValue = _cache.StringGet(cacheKey);
                    if (cachedValue.HasValue)
                    {
                        var cachedResult = JsonSerializer.Deserialize<OrderResponseViewModel>(cachedValue.ToString());
                        if (cachedResult != null)
                        {
                            return cachedResult;
                        }
                    }
                }
                catch
                {
                    // Fail silently to keep application running if Redis is down
                }
            }

            LazopRequest request = new LazopRequest("/order/get");
            request.SetHttpMethod("GET");
            request.AddApiParameter("order_id", param.OrderId);
            
            var response = _client.Execute(request, param.AccessToken);
            var result = new OrderResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };

            if (!result.IsError() && _cache != null)
            {
                try
                {
                    string json = JsonSerializer.Serialize(result);
                    _cache.StringSet(cacheKey, json, TimeSpan.FromMinutes(10));
                }
                catch
                {
                    // Fail silently
                }
            }

            return result;
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

        public OrderSyncState SyncHistoricalOrders(string accessToken, int daysBack)
        {
            string stateKey = $"lazada:sync:orders:state:{accessToken.Substring(0, Math.Min(10, accessToken.Length))}";
            OrderSyncState? state = null;

            if (_cache != null)
            {
                try
                {
                    var cachedState = _cache.StringGet(stateKey);
                    if (cachedState.HasValue)
                    {
                        state = JsonSerializer.Deserialize<OrderSyncState>(cachedState.ToString());
                    }
                }
                catch
                {
                    // Fail silently if Redis is down
                }
            }

            // If no active/unfinished job exists, initialize a new one
            if (state == null || state.Status == "Completed")
            {
                state = new OrderSyncState
                {
                    JobId = Guid.NewGuid().ToString(),
                    AccessToken = accessToken,
                    RangeStart = DateTime.UtcNow.AddDays(-daysBack),
                    RangeEnd = DateTime.UtcNow,
                    CurrentCreatedAfter = DateTime.UtcNow.AddDays(-daysBack),
                    Status = "Running",
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                // Resume existing job
                state.Status = "Running";
                state.UpdatedAt = DateTime.UtcNow;
            }

            // Save status = Running
            SaveSyncState(stateKey, state);

            try
            {
                bool hasMore = true;
                int safetyCounter = 0; // Prevent infinite loops in testing
                
                while (hasMore && safetyCounter < 10) // Limit to 10 batches per request for demonstration/safety
                {
                    safetyCounter++;

                    // Call Lazada Orders Get
                    var ordersResult = GetOrders(new GetOrdersRequestModel
                    {
                        AccessToken = accessToken,
                        CreatedAfter = state.CurrentCreatedAfter,
                        Limit = 20
                    });

                    if (ordersResult.IsError())
                    {
                        throw new Exception($"Lazada API Error: {ordersResult.Message} (Code: {ordersResult.Code})");
                    }

                    if (string.IsNullOrWhiteSpace(ordersResult.Body))
                    {
                        hasMore = false;
                        break;
                    }

                    // Parse orders
                    var ordersNode = System.Text.Json.Nodes.JsonNode.Parse(ordersResult.Body);
                    var ordersArray = ordersNode?["data"]?["orders"]?.AsArray();

                    if (ordersArray == null || ordersArray.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    // Save orders to DB / InMemoryStorage
                    foreach (var orderNode in ordersArray)
                    {
                        if (orderNode is System.Text.Json.Nodes.JsonObject orderObj)
                        {
                            var orderId = orderObj["order_id"]?.ToString();
                            var status = orderObj["statuses"]?[0]?.ToString() ?? orderObj["status"]?.ToString() ?? "UNKNOWN";
                            if (orderId != null)
                            {
                                var order = Lazop.Service.ImplementServices.WebhookServices.InMemoryStorage.LazadaOrders.GetOrAdd(orderId, id => new Lazop.Domain.Models.LazadaOrder { Id = id });
                                order.Status = status;
                            }
                        }
                    }

                    // Find the latest created_at date from the batch to advance our cursor
                    DateTime latestCreatedAt = state.CurrentCreatedAfter;
                    foreach (var orderNode in ordersArray)
                    {
                        var createdAtNode = orderNode?["created_at"];
                        if (createdAtNode != null && DateTime.TryParse(createdAtNode.ToString(), out DateTime orderCreated))
                        {
                            if (orderCreated > latestCreatedAt)
                            {
                                latestCreatedAt = orderCreated;
                            }
                        }
                    }

                    // If the cursor did not advance, we must add 1 second to avoid infinite loop
                    if (latestCreatedAt == state.CurrentCreatedAfter)
                    {
                        latestCreatedAt = latestCreatedAt.AddSeconds(1);
                    }

                    state.CurrentCreatedAfter = latestCreatedAt;
                    state.UpdatedAt = DateTime.UtcNow;

                    // Update progress in Redis
                    SaveSyncState(stateKey, state);

                    // SIMULATION OF CRASH for verification:
                    // If daysBack is 999 and we processed 2 pages, simulate a crash (exception)
                    if (daysBack == 999 && safetyCounter == 2)
                    {
                        throw new Exception("Simulated network failure / rate limit hit midway through synchronization!");
                    }
                }

                if (!hasMore)
                {
                    state.Status = "Completed";
                    state.UpdatedAt = DateTime.UtcNow;
                    SaveSyncState(stateKey, state);
                }
            }
            catch (Exception ex)
            {
                state.Status = "Failed";
                state.LastError = ex.Message;
                state.UpdatedAt = DateTime.UtcNow;
                SaveSyncState(stateKey, state);
                throw;
            }

            return state;
        }

        private void SaveSyncState(string key, OrderSyncState state)
        {
            if (_cache != null)
            {
                try
                {
                    string json = JsonSerializer.Serialize(state);
                    _cache.StringSet(key, json);
                }
                catch
                {
                    // Fail silently
                }
            }
        }
    }
}
