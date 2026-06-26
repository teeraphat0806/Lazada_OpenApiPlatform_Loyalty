using System;

namespace Lazop.Domain.Models
{
    public class LazadaMessage
    {
        public long Id { get; set; }
        public string Action { get; set; } = "";
        public string Response { get; set; } = "";
        public DateTime CreatedTime { get; set; }
    }
}
