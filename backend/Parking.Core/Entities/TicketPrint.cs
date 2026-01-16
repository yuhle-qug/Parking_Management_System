using System;

namespace Parking.Core.Entities
{
    public class TicketPrintData
    {
        public string TicketId { get; set; } = string.Empty;
        public string GateId { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
    }

    public class TicketPrintResult
    {
        public string Html { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/html";
        public string FileName { get; set; } = "ticket.html";
    }
}
