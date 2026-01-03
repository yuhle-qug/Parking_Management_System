using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
	// File-backed implementation storing sessions in DataStore/sessions.json.
	public class ParkingSessionRepository : BaseJsonRepository<ParkingSession>, IParkingSessionRepository
	{
		public ParkingSessionRepository() : base("sessions.json") { }

		public async Task<IEnumerable<ParkingSession>> FindActiveByPlateAsync(string plateNumber)
		{
			var list = await GetAllAsync();
			var result = list.Where(s => s.Vehicle?.LicensePlate == plateNumber && s.Status == "Active");
			return result;
		}

		public async Task<ParkingSession?> FindByTicketIdAsync(string ticketId)
		{
			var list = await GetAllAsync();
			return list.FirstOrDefault(s => s.Ticket?.TicketId == ticketId);
		}
	}
}
