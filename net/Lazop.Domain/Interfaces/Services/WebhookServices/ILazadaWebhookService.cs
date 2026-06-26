using Lazop.Domain.RequestModels.WebhookRequestModels;

namespace Lazop.Domain.Interfaces.Services.WebhookServices
{
    public interface ILazadaWebhookService
    {
        void CreateOrUpdateOrder(LazadaWebhookRequest data);
        void CreateOrUpdateReverseOrder(LazadaWebhookRequest data);
    }
}
