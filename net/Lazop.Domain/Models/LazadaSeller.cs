using System;
using Lazop.Domain.Enums;

namespace Lazop.Domain.Models
{
    public class LazadaSeller
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string? CompanyName { get; set; }
        public string? ShortCode { get; set; }
        public string? Location { get; set; }
        public string? CountryCode { get; set; }
        public Affirmative Verified { get; set; } = Affirmative.No;
        public ActiveStatus Status { get; set; } = ActiveStatus.Others;
        public Affirmative CrossBorder { get; set; } = Affirmative.No;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for C# usage
        public LazadaAccessToken? AccessToken { get; set; }
    }
}
