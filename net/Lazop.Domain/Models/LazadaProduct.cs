using System;

namespace Lazop.Domain.Models
{
    public class LazadaProduct
    {
        public long Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Status { get; set; }
        public long? PrimaryCategory { get; set; }
        public string? Description { get; set; }
        public string? AttributesJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
