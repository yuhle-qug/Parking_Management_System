using System;

namespace Parking.Core.Entities
{
    public class Customer
    {
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IdentityNumber { get; set; }
    }

    public class MonthlyTicket
    {
        public string TicketId { get; set; }
        public string CustomerId { get; set; }
        public string VehiclePlate { get; set; }
        public string VehicleType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double MonthlyFee { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; } = "PendingExternal";
        public string TransactionCode { get; set; } = string.Empty;
        public string? QrContent { get; set; }
        public string? ProviderLog { get; set; }
        public int PaymentAttempts { get; set; } = 0;

        public bool IsValid()
        {
            return Status == "Active" && DateTime.Now <= ExpiryDate;
        }
    }

    public class MembershipHistory
    {
        public string HistoryId { get; set; }
        public string TicketId { get; set; }
        public string Action { get; set; } // Extend / Cancel
        public int Months { get; set; }
        public double Amount { get; set; }
        public string PerformedBy { get; set; }
        public DateTime Time { get; set; }
        public string? Note { get; set; }
    }
}
