using System;

namespace Lazop.Domain.RequestModels.OrderRequestModels
{
    public class OrderSyncState
    {
        public string JobId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public DateTime CurrentCreatedAfter { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
        public string? LastError { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
