using Lazop.Domain.ViewModels;

namespace Lazop.Domain.Interfaces.Services.AuthServices
{
    public interface IAuthService
    {
        string GetAuthorizationUrl();
        LazopResponse CreateToken(string code);
        LazopResponse RefreshToken(string sellerShortCode);
    }
}
