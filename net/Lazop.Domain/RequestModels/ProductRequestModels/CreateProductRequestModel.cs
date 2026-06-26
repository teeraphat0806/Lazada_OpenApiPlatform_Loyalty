namespace Lazop.Domain.RequestModels.ProductRequestModels
{
    public class CreateProductRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string PayloadXml { get; set; } = string.Empty;
    }
}
