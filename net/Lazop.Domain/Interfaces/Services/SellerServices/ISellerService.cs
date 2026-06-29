using Lazop.Domain.Models;
using Lazop.Domain.ViewModels;

namespace Lazop.Domain.Interfaces.Services.SellerServices
{
    public interface ISellerService
    {
        LazopResponse GetSellerProfile(string sellerShortCode);
        LazadaSeller? GetLocalSellerInfo(string sellerShortCode);
    }
}
