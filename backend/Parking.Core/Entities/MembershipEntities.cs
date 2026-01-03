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

        public bool IsValid()
        {
            return Status == "Active" && DateTime.Now <= ExpiryDate;
        }
    }
}
