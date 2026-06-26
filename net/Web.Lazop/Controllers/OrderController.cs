using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Lazop.Domain.Interfaces.Services.OrderServices;
using Lazop.Domain.RequestModels.OrderRequestModels;

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
    }
}
