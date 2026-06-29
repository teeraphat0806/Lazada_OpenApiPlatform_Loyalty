using System;

namespace Lazop.Domain.Models
{
    public class LazadaProductSku
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string? SellerSku { get; set; }
        public string? ShopSku { get; set; }
        public string? Status { get; set; }
        public int? Price { get; set; }
        public int? Quantity { get; set; }
        public int? Available { get; set; }
        public string? Variation { get; set; }
        public string? ColorFamily { get; set; }
        public string? ImagesJson { get; set; }
        public string? MultiWarehouseInventoriesJson { get; set; }
        public string? SalePropJson { get; set; }
        public int? PackageWidth { get; set; }
        public int? PackageHeight { get; set; }
        public int? PackageLength { get; set; }
        public int? PackageWeight { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
