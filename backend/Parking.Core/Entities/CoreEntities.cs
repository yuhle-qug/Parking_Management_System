using System;

namespace Parking.Core.Entities
{
    // Vé xe
    public class Ticket
    {
        public string TicketId { get; set; }
        public DateTime IssueTime { get; set; }
        public string GateId { get; set; }
        public string? CardId { get; set; }
        public string TicketType { get; set; } = "Daily"; // "Daily" or "Monthly"

        // [Domain Logic] Check if the ticket is valid based on issuance time
        public bool IsValid()
        {
            // Example Rule: Ticket cannot be from the future
            if (IssueTime > DateTime.Now) return false;
            
            // Example Rule: Ticket ID must be present
            if (string.IsNullOrWhiteSpace(TicketId)) return false;

            return true;
        }
    }

    // Thanh toán
    public class Payment
    {
        public string PaymentId { get; set; }
        public double Amount { get; set; }
        public DateTime Time { get; set; }
        public string Method { get; set; } // Online methods only (e.g., QR)
        public string Status { get; set; } // Pending, Completed, Failed, Cancelled
        public string TransactionCode { get; set; } = string.Empty; // Mã giao dịch từ gateway
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; }
        public string? ProviderLog { get; set; }

        public void MarkCompleted()
        {
            Status = "Completed";
            Time = DateTime.Now;
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public string? Error { get; set; }
        public int Attempts { get; set; }
        public string? QrContent { get; set; }
        public string? ProviderLog { get; set; }
        public string? Message { get; set; }
    }

    // Phiên gửi xe (Quan trọng nhất)
    public class ParkingSession
    {
        public string SessionId { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; } // Nullable vì xe đang gửi chưa ra
        public double FeeAmount { get; set; }
        public double? BaseFee { get; set; }
        public double? LostTicketFee { get; set; }
        public string Status { get; set; } // Active, Completed, LostTicket
        public string? CardId { get; set; }

        // [OOP - Composition/Aggregation]: Các đối tượng liên quan
        public Vehicle Vehicle { get; set; }
        public Ticket Ticket { get; set; }
        public Payment Payment { get; set; } // 0..1 (Có thể chưa thanh toán)
        public string ParkingZoneId { get; set; } // Foreign Key logic

        public void SetExitTime(DateTime time)
        {
            ExitTime = time;
        }

        public string? ExitGateId { get; set; }

        public void Close()
        {
            Status = "Completed";
        }

        public void AttachPayment(Payment payment)
        {
            this.Payment = payment;
        }
    }
}