using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Lazop.Domain.Interfaces.Services.ProductServices;
using Lazop.Domain.RequestModels.ProductRequestModels;

namespace Web.Lazop.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public IActionResult GetProducts([FromQuery] GetProductsRequestModel param)
        {
            if (string.IsNullOrWhiteSpace(param.AccessToken))
            {
                return BadRequest(new { Message = "AccessToken is required" });
            }

            var result = _productService.GetProducts(param);
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

        [HttpGet("{itemId}")]
        public IActionResult GetProduct([FromQuery] string accessToken, string itemId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return BadRequest(new { Message = "AccessToken is required in query" });
            }

            var param = new GetProductRequestModel
            {
                AccessToken = accessToken,
                ItemId = itemId
            };

            var result = _productService.GetProduct(param);
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

        [HttpPost]
        public IActionResult CreateProduct([FromBody] CreateProductRequestModel param)
        {
            if (string.IsNullOrWhiteSpace(param.AccessToken))
            {
                return BadRequest(new { Message = "AccessToken is required" });
            }

            var result = _productService.CreateProduct(param);
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
