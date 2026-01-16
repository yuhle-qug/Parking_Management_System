using System;

namespace Parking.Core.Entities
{
    public class PaymentGatewayResult
    {
        public bool Accepted { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string QrContent { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public string? ProviderMessage { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
