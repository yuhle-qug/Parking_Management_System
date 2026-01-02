using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
	// In-memory implementation for demo/testing purposes.
	public class ParkingSessionRepository : IParkingSessionRepository
	{
		private static readonly List<ParkingSession> Sessions = new();

		public Task AddAsync(ParkingSession session)
		{
			Sessions.Add(session);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<ParkingSession>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<ParkingSession>>(Sessions);
		}

		public Task<ParkingSession?> GetByIdAsync(string id)
		{
			var session = Sessions.FirstOrDefault(s => s.SessionId == id);
			return Task.FromResult(session);
		}

		public Task UpdateAsync(ParkingSession session)
		{
			var index = Sessions.FindIndex(s => s.SessionId == session.SessionId);
			if (index >= 0)
			{
				Sessions[index] = session;
			}
			return Task.CompletedTask;
		}

		public Task<IEnumerable<ParkingSession>> FindActiveByPlateAsync(string plateNumber)
		{
			var result = Sessions.Where(s => s.Vehicle?.LicensePlate == plateNumber && s.Status == "Active");
			return Task.FromResult<IEnumerable<ParkingSession>>(result);
		}

		public Task<ParkingSession?> FindByTicketIdAsync(string ticketId)
		{
			var session = Sessions.FirstOrDefault(s => s.Ticket?.TicketId == ticketId);
			return Task.FromResult(session);
		}
	}
}
