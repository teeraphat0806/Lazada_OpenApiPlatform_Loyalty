using Lazop.Domain.RequestModels.ProductRequestModels;
using Lazop.Domain.ViewModels.ProductViewModels;

namespace Lazop.Domain.Interfaces.Services.ProductServices
{
    public interface IProductService
    {
        ProductResponseViewModel GetProduct(GetProductRequestModel param);
        ProductResponseViewModel GetProducts(GetProductsRequestModel param);
        ProductResponseViewModel CreateProduct(CreateProductRequestModel param);
    }
}
