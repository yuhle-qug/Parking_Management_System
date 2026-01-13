using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IMonthlyTicketRepository _ticketRepo;
        private readonly IMembershipPolicyRepository _policyRepo;

        public MembershipService(ICustomerRepository customerRepo, IMonthlyTicketRepository ticketRepo, IMembershipPolicyRepository policyRepo)
        {
            _customerRepo = customerRepo;
            _ticketRepo = ticketRepo;
            _policyRepo = policyRepo;
        }

        public async Task<MonthlyTicket> RegisterMonthlyTicketAsync(Customer customerInfo, Vehicle vehicle, string planId)
        {
            var existingCustomer = await _customerRepo.FindByPhoneAsync(customerInfo.Phone);
            if (existingCustomer == null)
            {
                customerInfo.CustomerId = Guid.NewGuid().ToString();
                await _customerRepo.AddAsync(customerInfo);
                existingCustomer = customerInfo;
            }

            var existingTicket = await _ticketRepo.FindActiveByPlateAsync(vehicle.LicensePlate);
            if (existingTicket != null)
            {
                throw new InvalidOperationException($"Xe {vehicle.LicensePlate} đã có vé tháng hiệu lực.");
            }

            var vehicleType = vehicle switch
            {
                ElectricCar => "ELECTRIC_CAR",
                Car => "CAR",
                ElectricMotorbike => "ELECTRIC_MOTORBIKE",
                Motorbike => "MOTORBIKE",
                ElectricBicycle => "ELECTRIC_BICYCLE",
                Bicycle => "BICYCLE",
                _ => vehicle.GetType().Name.ToUpperInvariant()
            };

            var policy = await _policyRepo.GetPolicyAsync(vehicleType);
            var fee = policy?.MonthlyPrice ?? 2_000_000;

            var newTicket = new MonthlyTicket
            {
                TicketId = "M-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CustomerId = existingCustomer.CustomerId,
                VehiclePlate = vehicle.LicensePlate,
                VehicleType = vehicleType,
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                Status = "Active",
                MonthlyFee = fee
            };

            await _ticketRepo.AddAsync(newTicket);
            return newTicket;
        }

        public async Task<bool> ExtendMonthlyTicketAsync(string ticketId, int months)
        {
            var ticket = await _ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null) return false;

            ticket.ExpiryDate = ticket.ExpiryDate.AddMonths(months);
            await _ticketRepo.UpdateAsync(ticket);
            return true;
        }

        // [NEW] Implement hàm lấy danh sách vé
        public async Task<IEnumerable<MonthlyTicket>> GetAllTicketsAsync()
        {
            var tickets = await _ticketRepo.GetAllAsync();
            var now = DateTime.Now;

            // Chỉ trả về vé còn hiệu lực tại thời điểm gọi API
            return tickets.Where(t => t.Status == "Active" && t.ExpiryDate >= now);
        }

        // [NEW] Implement hàm lấy bảng giá
        public async Task<IEnumerable<MembershipPolicy>> GetAllPoliciesAsync()
        {
            return await _policyRepo.GetAllAsync();
        }
    }
}
