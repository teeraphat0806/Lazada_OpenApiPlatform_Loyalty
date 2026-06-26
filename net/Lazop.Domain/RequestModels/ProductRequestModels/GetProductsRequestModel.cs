namespace Lazop.Domain.RequestModels.ProductRequestModels
{
    public class GetProductsRequestModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Filter { get; set; } = "all";
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 20;
    }
}
