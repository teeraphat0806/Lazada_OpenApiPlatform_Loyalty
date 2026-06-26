using System.Collections.Generic;
using Lazop.Domain.Interfaces;
using Lazop.Domain.Interfaces.Services.ProductServices;
using Lazop.Domain.RequestModels;
using Lazop.Domain.RequestModels.ProductRequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Domain.ViewModels.ProductViewModels;

namespace Lazop.Service.ImplementServices.ProductServices
{
    /// <summary>
    /// Service wrapper for Lazada Product APIs (/product/*).
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ILazopClient _client;

        public ProductService(ILazopClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Retrieves detail information of a single product item.
        /// API path: /product/get
        /// </summary>
        public ProductResponseViewModel GetProduct(GetProductRequestModel param)
        {
            LazopRequest request = new LazopRequest("/product/get");
            request.SetHttpMethod("GET");
            request.AddApiParameter("item_id", param.ItemId);
            
            var response = _client.Execute(request, param.AccessToken);
            return new ProductResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };
        }

        /// <summary>
        /// Retrieves a list of products.
        /// API path: /products/get
        /// </summary>
        public ProductResponseViewModel GetProducts(GetProductsRequestModel param)
        {
            LazopRequest request = new LazopRequest("/products/get");
            request.SetHttpMethod("GET");
            request.AddApiParameter("filter", param.Filter);
            request.AddApiParameter("offset", param.Offset.ToString());
            request.AddApiParameter("limit", param.Limit.ToString());
            
            var response = _client.Execute(request, param.AccessToken);
            return new ProductResponseViewModel
            {
                Code = response.Code,
                Type = response.Type,
                Message = response.Message,
                RequestId = response.RequestId,
                Body = response.Body
            };
        }

        /// <summary>
        /// Creates a new product item.
        /// API path: /product/create
        /// </summary>
        public ProductResponseViewModel CreateProduct(CreateProductRequestModel param)
        {
            LazopRequest request = new LazopRequest("/product/create");
            request.SetHttpMethod("POST");
            request.AddApiParameter("payload", param.PayloadXml);
            
            var response = _client.Execute(request, param.AccessToken);
            return new ProductResponseViewModel
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
