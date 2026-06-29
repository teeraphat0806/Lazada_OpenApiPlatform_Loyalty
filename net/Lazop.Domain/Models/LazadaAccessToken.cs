using System;

namespace Lazop.Domain.Models
{
    public class LazadaAccessToken
    {
        public long Id { get; set; }
        public long SellerId { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
        public string? UserInfoJson { get; set; } // stores string JSON of user info
        public string? CountryCode { get; set; }
        public string? AccountId { get; set; }
        public string? Account { get; set; }
        public string? AccountPlatform { get; set; }
        public string? Code { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
