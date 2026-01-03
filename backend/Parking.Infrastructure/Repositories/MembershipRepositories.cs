using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class CustomerRepository : BaseJsonRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository() : base("customers.json") { }

        public async Task<Customer> FindByPhoneAsync(string phone)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(c => c.Phone == phone);
        }
    }

    public class MonthlyTicketRepository : BaseJsonRepository<MonthlyTicket>, IMonthlyTicketRepository
    {
        public MonthlyTicketRepository() : base("monthly_tickets.json") { }

        public async Task<MonthlyTicket> FindActiveByPlateAsync(string plate)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(t =>
                t.VehiclePlate.Equals(plate, StringComparison.OrdinalIgnoreCase) &&
                t.Status == "Active" &&
                t.ExpiryDate >= DateTime.Now);
        }

        public async Task<IEnumerable<MonthlyTicket>> FindExpiredTicketsAsync(DateTime date)
        {
            var list = await GetAllAsync();
            return list.Where(t => t.ExpiryDate < date && t.Status == "Active");
        }
    }
}
