using System;

namespace Parking.Core.Entities
{
    // Vé xe
    public class Ticket
    {
        public string TicketId { get; set; }
        public DateTime IssueTime { get; set; }
        public string GateId { get; set; }
    }

    // Thanh toán
    public class Payment
    {
        public string PaymentId { get; set; }
        public double Amount { get; set; }
        public DateTime Time { get; set; }
        public string Method { get; set; } // Cash, CreditCard, QR
        public string Status { get; set; } // Pending, Completed

        public void MarkCompleted()
        {
            Status = "Completed";
            Time = DateTime.Now;
        }
    }

    // Phiên gửi xe (Quan trọng nhất)
    public class ParkingSession
    {
        public string SessionId { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; } // Nullable vì xe đang gửi chưa ra
        public double FeeAmount { get; set; }
        public string Status { get; set; } // Active, Completed, LostTicket

        // [OOP - Composition/Aggregation]: Các đối tượng liên quan
        public Vehicle Vehicle { get; set; }
        public Ticket Ticket { get; set; }
        public Payment Payment { get; set; } // 0..1 (Có thể chưa thanh toán)
        public string ParkingZoneId { get; set; } // Foreign Key logic

        public void SetExitTime(DateTime time)
        {
            ExitTime = time;
        }

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