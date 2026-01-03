using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IMonthlyTicketRepository _ticketRepo;

        public MembershipService(ICustomerRepository customerRepo, IMonthlyTicketRepository ticketRepo)
        {
            _customerRepo = customerRepo;
            _ticketRepo = ticketRepo;
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

            var newTicket = new MonthlyTicket
            {
                TicketId = "M-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CustomerId = existingCustomer.CustomerId,
                VehiclePlate = vehicle.LicensePlate,
                VehicleType = vehicle.GetType().Name,
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                Status = "Active",
                MonthlyFee = 1_500_000
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
    }
}
