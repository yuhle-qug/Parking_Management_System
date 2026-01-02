using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
	public interface IParkingSessionRepository : IRepository<ParkingSession>
	{
		Task<IEnumerable<ParkingSession>> FindActiveByPlateAsync(string plateNumber);
		Task<ParkingSession?> FindByTicketIdAsync(string ticketId);
	}

	public interface IParkingZoneRepository : IRepository<ParkingZone>
	{
		Task<ParkingZone?> FindSuitableZoneAsync(string vehicleType, bool isElectric);
	}

	public interface ITicketRepository : IRepository<Ticket>
	{
	}
}
