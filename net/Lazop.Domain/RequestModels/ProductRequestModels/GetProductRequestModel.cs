namespace Lazop.Domain.RequestModels.ProductRequestModels
{
    public class GetProductRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
    }
}
