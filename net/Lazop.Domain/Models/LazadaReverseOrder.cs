namespace Lazop.Domain.Models
{
    public class LazadaReverseOrder
    {
        public string Id { get; set; } = "";
        public string? OrderId { get; set; }
        public string? SellerId { get; set; }
        public string? BuyerId { get; set; }
        public string? Status { get; set; }
    }
}
