using System;

namespace Parking.Core.Entities
{
    public class MonthlyTicketDto
    {
        public string TicketId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double MonthlyFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public string? QrContent { get; set; }
        public string? ProviderLog { get; set; }
        public int PaymentAttempts { get; set; }
    }
}
