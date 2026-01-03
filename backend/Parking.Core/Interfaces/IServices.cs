using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
	public interface IParkingService
	{
		Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId);
		Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId);
	}

	public interface IPaymentService
	{
		Task<bool> ProcessPaymentAsync(string sessionId, double amount, string method);
	}

	public interface IMembershipService
	{
		Task<MonthlyTicket> RegisterMonthlyTicketAsync(Customer customerInfo, Vehicle vehicle, string planId);
		Task<bool> ExtendMonthlyTicketAsync(string ticketId, int months);
	}

	// External device abstraction for opening gates.
	public interface IGateDevice
	{
		Task OpenGateAsync(string gateId);
	}

	// Payment gateway adapter contract.
	public interface IPaymentGateway
	{
		Task<bool> RequestPaymentAsync(double amount, string orderInfo);
	}
}
