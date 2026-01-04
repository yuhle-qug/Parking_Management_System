using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
	public interface ICustomerRepository : IRepository<Customer>
	{
		Task<Customer> FindByPhoneAsync(string phone);
	}

	public interface IMonthlyTicketRepository : IRepository<MonthlyTicket>
	{
		Task<MonthlyTicket> FindActiveByPlateAsync(string plate);
		Task<IEnumerable<MonthlyTicket>> FindExpiredTicketsAsync(DateTime date);
	}

	public interface IMembershipPolicyRepository : IRepository<MembershipPolicy>
	{
		Task<MembershipPolicy?> GetPolicyAsync(string vehicleType);
	}
}
