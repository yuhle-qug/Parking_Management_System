using System;
using System.Collections.Generic;

namespace Parking.Core.Entities
{
    // Khách hàng
    public class Customer
    {
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime RegisterDate { get; set; }
        public List<MonthlyTicket> Tickets { get; set; } = new List<MonthlyTicket>();
    }

    // Vé tháng
    public class MonthlyTicket
    {
        public string TicketId { get; set; }
        public string CustomerId { get; set; }
        public string LicensePlate { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string PlanId { get; set; } // Reference to pricing plan
        public bool IsActive => DateTime.Now <= ExpiryDate;

        public void Extend(int months)
        {
            ExpiryDate = ExpiryDate.AddMonths(months);
        }
    }

    // User Account (B?o v?, Admin)
    public class UserAccount
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Should be hashed in real scenario
        public string Role { get; set; } // Admin, SecurityGuard, Operator
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
