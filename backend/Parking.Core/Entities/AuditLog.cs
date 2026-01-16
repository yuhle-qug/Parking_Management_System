using System;

namespace Parking.Core.Entities
{
    public class AuditLog
    {
        public string LogId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Action { get; set; } // CheckIn, CheckOut, Payment, Login, ...
        public string Username { get; set; } // User who performed the action, or "System"
        public string? ReferenceId { get; set; } // SessionId, TicketId, etc.
        public string? Details { get; set; } // JSON payload or description
        public bool Success { get; set; } = true;
    }
}
