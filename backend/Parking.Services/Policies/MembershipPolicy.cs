using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Policies
{
    public class MembershipPolicy
    {
        private readonly ITimeProvider _timeProvider;

        public MembershipPolicy(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public bool CanRenew(MonthlyTicket ticket, int months)
        {
            if (ticket.Status == "Cancelled") return false;
            if (months < 1 || months > 12) return false;
            
            // Allow renewal within 7 days before expiry
            var daysUntilExpiry = (ticket.ExpiryDate - _timeProvider.Now).TotalDays;
            return daysUntilExpiry <= 7;
        }

        public bool CanCancel(MonthlyTicket ticket)
        {
            return ticket.Status == "Active" || ticket.Status == "PendingPayment";
        }
    }
}
