using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
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

        /// <summary>
        /// Retrieves a list of orders with their items.
        /// API path: /orders/get followed by /orders/items/get
        /// </summary>
        public OrderResponseViewModel GetOrdersWithItems(GetOrdersRequestModel param)
        {
            // 1. Fetch Orders
            var ordersResult = GetOrders(param);
            if (ordersResult.IsError() || string.IsNullOrWhiteSpace(ordersResult.Body))
            {
                return ordersResult;
            }

            // 2. Parse Orders using System.Text.Json.Nodes
            var ordersNode = JsonNode.Parse(ordersResult.Body);
            var ordersArray = ordersNode?["data"]?["orders"]?.AsArray();

            if (ordersArray == null || ordersArray.Count == 0)
            {
                return ordersResult;
            }

            // 3. Extract Order IDs
            var orderIds = new List<long>();
            foreach (var order in ordersArray)
            {
                if (order?["order_id"] != null)
                {
                    var idToken = order["order_id"];
                    if (idToken != null && long.TryParse(idToken.ToString(), out long idVal))
                    {
                        orderIds.Add(idVal);
                    }
                }
            }

            if (orderIds.Count == 0)
            {
                return ordersResult;
            }

            // 4. Fetch Items for these Order IDs
            var itemsResult = GetMultipleOrderItems(new GetMultipleOrderItemsRequestModel
            {
                AccessToken = param.AccessToken,
                OrderIds = orderIds
            });

            if (itemsResult.IsError())
            {
                return itemsResult;
            }

            // 5. Parse and Group Items by order_id
            var itemsByOrderId = new Dictionary<string, List<JsonObject>>();
            var buyerIdByOrderId = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(itemsResult.Body))
            {
                var itemsNode = JsonNode.Parse(itemsResult.Body);
                var itemsArray = itemsNode?["data"]?.AsArray();

                if (itemsArray != null)
                {
                    foreach (var itemNode in itemsArray)
                    {
                        if (itemNode is JsonObject itemObj)
                        {
                            var orderIdNode = itemObj["order_id"];
                            if (orderIdNode != null)
                            {
                                string orderIdStr = orderIdNode.ToString();
                                
                                // Extract and remove buyer_id to place it in the parent order object
                                var buyerIdNode = itemObj["buyer_id"];
                                if (buyerIdNode != null)
                                {
                                    if (!buyerIdByOrderId.ContainsKey(orderIdStr))
                                    {
                                        buyerIdByOrderId[orderIdStr] = buyerIdNode.ToString();
                                    }
                                    itemObj.Remove("buyer_id");
                                }

                                // Remove the duplicate order_id from the nested item itself
                                itemObj.Remove("order_id");

                                if (!itemsByOrderId.ContainsKey(orderIdStr))
                                {
                                    itemsByOrderId[orderIdStr] = new List<JsonObject>();
                                }
                                itemsByOrderId[orderIdStr].Add(itemObj);
                            }
                        }
                    }
                }
            }

            // 6. Merge Items into corresponding Orders
            foreach (var orderNode in ordersArray)
            {
                if (orderNode is JsonObject orderObj)
                {
                    var orderIdVal = orderObj["order_id"]?.ToString();
                    
                    // Inject buyer_id at order level if found in the items
                    if (orderIdVal != null && buyerIdByOrderId.TryGetValue(orderIdVal, out var buyerId))
                    {
                        if (long.TryParse(buyerId, out long parsedBuyerId))
                        {
                            orderObj["buyer_id"] = parsedBuyerId;
                        }
                        else
                        {
                            orderObj["buyer_id"] = buyerId;
                        }
                    }

                    var itemsForOrder = new JsonArray();
                    if (orderIdVal != null && itemsByOrderId.TryGetValue(orderIdVal, out var itemsList))
                    {
                        foreach (var item in itemsList)
                        {
                            // Clone the node to detach it from its previous parent array
                            var itemClone = JsonNode.Parse(item.ToJsonString())?.AsObject();
                            if (itemClone != null)
                            {
                                itemsForOrder.Add(itemClone);
                            }
                        }
                    }
                    
                    // Add to the order object
                    orderObj["items"] = itemsForOrder;
                }
            }

            return new OrderResponseViewModel
            {
                Type = ordersResult.Type,
                Code = ordersResult.Code,
                Message = ordersResult.Message,
                RequestId = ordersResult.RequestId,
                Body = ordersNode?.ToJsonString() ?? string.Empty
            };
        }
    }
}
