using System;
using System.Text.RegularExpressions;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IMembershipValidator
    {
        bool CanRenew(MonthlyTicket ticket, int months);
        bool CanCancel(MonthlyTicket ticket);
        bool IsValidPlan(int months);
        void ValidateCustomerInfo(Customer customer);
        void ValidateVehicleInfo(Vehicle vehicle);
    }
}
