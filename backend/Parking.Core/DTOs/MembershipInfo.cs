using System;
using System.Collections.Generic;

namespace Parking.Core.DTOs
{
    public class MembershipInfo
    {
        public bool HasMonthlyTicket { get; set; }
        public bool IsValid { get; set; }
        public string? TicketId { get; set; }
        public string? VehiclePlate { get; set; }
        public string? VehicleType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Plan { get; set; }
        public string? Status { get; set; }
    }
}
