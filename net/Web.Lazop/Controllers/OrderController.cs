using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Lazop.Domain.Interfaces.Services.OrderServices;
using Lazop.Domain.RequestModels.OrderRequestModels;
//1113142529964242
namespace Web.Lazop.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public IActionResult GetOrders([FromQuery] GetOrdersRequestModel param)
        {
            if (string.IsNullOrWhiteSpace(param.AccessToken))
            {
                return BadRequest(new { Message = "AccessToken is required" });
            }

            var result = _orderService.GetOrders(param);
            if (result.IsError())
            {
                return BadRequest(result);
            }

            object? parsedBody = null;
            if (!string.IsNullOrWhiteSpace(result.Body))
            {
                try
                {
                    parsedBody = JsonSerializer.Deserialize<object>(result.Body);
                }
                catch
                {
                    parsedBody = result.Body;
                }
            }

            return Ok(new
            {
                result.Type,
                result.Code,
                result.Message,
                result.RequestId,
                Body = parsedBody
            });
        }

        [HttpGet("{orderId}")]
        public IActionResult GetOrderDetail([FromQuery] string accessToken, string orderId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { Message = "AccessToken is required in query" });
            }

            var param = new GetOrderDetailRequestModel
            {
                AccessToken = accessToken,
                OrderId = orderId
            };

            var result = _orderService.GetOrderDetail(param);
            if (result.IsError())
            {
                return BadRequest(result);
            }

            object? parsedBody = null;
            if (!string.IsNullOrWhiteSpace(result.Body))
            {
                try
                {
                    parsedBody = JsonSerializer.Deserialize<object>(result.Body);
                }
                catch
                {
                    parsedBody = result.Body;
                }
            }

            return Ok(new
            {
                result.Type,
                result.Code,
                result.Message,
                result.RequestId,
                Body = parsedBody
            });
        }

        [HttpGet("with-items")]
        public IActionResult GetOrdersWithItems([FromQuery] GetOrdersRequestModel param)
        {
            if (string.IsNullOrWhiteSpace(param.AccessToken))
            {
                return BadRequest(new { Message = "AccessToken is required" });
            }

            // 1. Fetch Orders
            var ordersResult = _orderService.GetOrders(param);
            if (ordersResult.IsError())
            {
                return BadRequest(ordersResult);
            }

            if (string.IsNullOrWhiteSpace(ordersResult.Body))
            {
                return Ok(new
                {
                    ordersResult.Type,
                    ordersResult.Code,
                    ordersResult.Message,
                    ordersResult.RequestId,
                    Body = (object?)null
                });
            }

            try
            {
                // 2. Parse Orders using System.Text.Json.Nodes
                var ordersNode = JsonNode.Parse(ordersResult.Body);
                var ordersArray = ordersNode?["data"]?["orders"]?.AsArray();

                if (ordersArray == null || ordersArray.Count == 0)
                {
                    // No orders, return as is
                    return Ok(new
                    {
                        ordersResult.Type,
                        ordersResult.Code,
                        ordersResult.Message,
                        ordersResult.RequestId,
                        Body = ordersNode
                    });
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
                    return Ok(new
                    {
                        ordersResult.Type,
                        ordersResult.Code,
                        ordersResult.Message,
                        ordersResult.RequestId,
                        Body = ordersNode
                    });
                }

                // 4. Fetch Items for these Order IDs
                var itemsResult = _orderService.GetMultipleOrderItems(new GetMultipleOrderItemsRequestModel
                {
                    AccessToken = param.AccessToken,
                    OrderIds = orderIds
                });

                if (itemsResult.IsError())
                {
                    return BadRequest(itemsResult);
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

                return Ok(new
                {
                    ordersResult.Type,
                    ordersResult.Code,
                    ordersResult.Message,
                    ordersResult.RequestId,
                    Body = ordersNode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error merging orders and items: {ex.Message}" });
            }
        }

        [HttpPost("sync-historical")]
        public IActionResult SyncHistoricalOrders([FromQuery] string accessToken, [FromQuery] int daysBack = 30)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { Message = "AccessToken is required" });
            }

            try
            {
                var result = _orderService.SyncHistoricalOrders(accessToken, daysBack);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = ex.Message,
                    Detail = "Synchronization failed midway. Checkpoint state has been saved to Redis. Try calling this endpoint again with the same parameters to resume."
                });
            }
        }
    }
}
