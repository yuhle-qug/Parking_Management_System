using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class CustomerRepository : BaseJsonRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "customers.json") { }

        public async Task<Customer> FindByPhoneAsync(string phone)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(c => c.Phone == phone);
        }
    }

    public class MonthlyTicketRepository : BaseJsonRepository<MonthlyTicket>, IMonthlyTicketRepository
    {
        public MonthlyTicketRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "monthly_tickets.json") { }

        public async Task<MonthlyTicket> FindActiveByPlateAsync(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return null;
            
            // Normalize input: Remove spaces, dashes, dots, etc. Keep only alphanumeric.
            string Normalize(string input) 
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                return new string(input.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
            }

            var cleanPlate = Normalize(plate);
            var list = await GetAllAsync();
            
            return list.FirstOrDefault(t =>
                Normalize(t.VehiclePlate) == cleanPlate &&
                t.Status == "Active" &&
                t.ExpiryDate >= DateTime.Now);
        }

        public async Task<IEnumerable<MonthlyTicket>> FindExpiredTicketsAsync(DateTime date)
        {
            var list = await GetAllAsync();
            return list.Where(t => t.ExpiryDate < date && t.Status == "Active");
        }
    }

    public class MembershipHistoryRepository : BaseJsonRepository<MembershipHistory>, IMembershipHistoryRepository
    {
        public MembershipHistoryRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "membership_history.json") { }

        public async Task<IEnumerable<MembershipHistory>> GetHistoryAsync(string ticketId)
        {
            var list = await GetAllAsync();
            return list
                .Where(h => h.TicketId.Equals(ticketId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.Time);
        }

        public async Task AddHistoryAsync(MembershipHistory history)
        {
            await AddAsync(history);
        }
    }
}
