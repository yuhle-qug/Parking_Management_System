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
        public string? VehicleBrand { get; set; }
        public string? VehicleColor { get; set; }
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

        public void Activate()
        {
            if (Status == "Active") return; // Idempotent
            
            Status = "Active";
            PaymentStatus = "Completed";
            if (StartDate > DateTime.Now)
            {
                StartDate = DateTime.Now;
            }
        }

        public void Cancel(string reason)
        {
            Status = "Cancelled";
            PaymentStatus = "Cancelled";
            ExpiryDate = DateTime.Now;
            ProviderLog = string.IsNullOrWhiteSpace(ProviderLog) ? reason : $"{ProviderLog} | {reason}";
        }

        public void SetPendingPayment(string transactionCode, string qrContent, string providerLog)
        {
            Status = "PendingPayment";
            PaymentStatus = "PendingExternal";
            TransactionCode = string.IsNullOrWhiteSpace(transactionCode) 
                ? $"MT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}" 
                : transactionCode;
            QrContent = qrContent;
            ProviderLog = providerLog;
            PaymentAttempts++;
        }

        public void SetPaymentFailed(string reason)
        {
            Status = "PaymentFailed";
            PaymentStatus = "Failed";
            ProviderLog = reason;
            PaymentAttempts++;
        }

        public void PrepareExtend(DateTime baseDate, int months, double newFee)
        {
            ExpiryDate = baseDate.AddMonths(months);
            Status = "PendingPayment";
            PaymentStatus = "PendingExternal";
            PaymentAttempts = 0;
            TransactionCode = string.Empty;
            QrContent = null;
            ProviderLog = null;
            MonthlyFee = newFee;
        }

        public void ApproveCancel(string adminId)
        {
            Status = "Cancelled";
            PaymentStatus = "Cancelled";
            ExpiryDate = DateTime.Now;
            ProviderLog = $"{ProviderLog} | Approved by {adminId}";
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
