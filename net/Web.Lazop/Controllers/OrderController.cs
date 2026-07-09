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

            try
            {
                var result = _orderService.GetOrdersWithItems(param);
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
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error merging orders and items: {ex.Message}" });
            }
        }
    }
}
