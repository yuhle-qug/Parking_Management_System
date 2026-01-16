using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
	public interface IParkingSessionRepository : IRepository<ParkingSession>
	{
		Task<IEnumerable<ParkingSession>> FindActiveByPlateAsync(string plateNumber);
		Task<ParkingSession?> FindByTicketIdAsync(string ticketId);
		Task<int> CountActiveByZoneAsync(string zoneId);
	}

	public interface IParkingZoneRepository : IRepository<ParkingZone>
	{
		Task<ParkingZone?> FindSuitableZoneAsync(string vehicleType, bool isElectric, string gateId);
	}

	public interface IPricePolicyRepository : IRepository<PricePolicy>
	{
		Task<PricePolicy?> GetPolicyAsync(string policyId);
		Task<PricePolicy?> GetPolicyByVehicleTypeAsync(string vehicleType);
	}

	public interface ITicketRepository : IRepository<Ticket>
	{
	}

	public interface IGateRepository : IRepository<Gate>
	{
		// Custom query to find gates supporting a vehicle type
		Task<IEnumerable<Gate>> GetGatesForVehicleAsync(string vehicleCategory);
	}
}
