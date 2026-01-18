using System;
using System.Text.RegularExpressions;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Validators
{
    public class MembershipValidator : IMembershipValidator
    {
        private readonly ITimeProvider _timeProvider;

        public MembershipValidator(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public bool CanRenew(MonthlyTicket ticket, int months)
        {
            if (ticket.Status == "Cancelled") return false;
            if (!IsValidPlan(months)) return false;
            
            // Allow renewal within 7 days before expiry
            var daysUntilExpiry = (ticket.ExpiryDate - _timeProvider.Now).TotalDays;
            return daysUntilExpiry <= 7;
        }

        public bool CanCancel(MonthlyTicket ticket)
        {
            return ticket.Status == "Active" || ticket.Status == "PendingPayment";
        }

        public bool IsValidPlan(int months)
        {
            return months >= 1 && months <= 12;
        }

        public void ValidateCustomerInfo(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Name))
                throw new ArgumentException("Tên khách hàng không được để trống");
            
            if (string.IsNullOrWhiteSpace(customer.Phone))
                throw new ArgumentException("Số điện thoại không được để trống");
            
            // Validate Vietnamese phone format
            var phoneRegex = new Regex(@"^(0|\+84)[0-9]{9}$");
            if (!phoneRegex.IsMatch(customer.Phone))
                throw new ArgumentException("Số điện thoại không hợp lệ (định dạng: 0xxxxxxxxx hoặc +84xxxxxxxxx)");
        }

        public void ValidateVehicleInfo(Vehicle vehicle)
        {
            if (vehicle.LicensePlate == null || string.IsNullOrWhiteSpace(vehicle.LicensePlate.Value))
                throw new ArgumentException("Biển số xe không được để trống");
        }
    }
}
