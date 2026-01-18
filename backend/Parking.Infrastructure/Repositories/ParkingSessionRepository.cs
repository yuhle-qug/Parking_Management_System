using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
	// File-backed implementation storing sessions in DataStore/sessions.json.
	public class ParkingSessionRepository : BaseJsonRepository<ParkingSession>, IParkingSessionRepository
	{
		public ParkingSessionRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "sessions.json") { }

		private static string NormalizePlate(string? plate)
		{
			if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
			var normalized = new string(plate.Where(char.IsLetterOrDigit).ToArray());
			return normalized.Trim().ToUpperInvariant();
		}

		public async Task<IEnumerable<ParkingSession>> FindActiveByPlateAsync(string plateNumber)
		{
			var list = await GetAllAsync();
			var target = NormalizePlate(plateNumber);
			// Include both Active and PendingPayment for lost ticket scenarios
			var result = list.Where(s =>
				NormalizePlate(s.Vehicle?.LicensePlate?.Value) == target &&
				(s.Status == "Active" || s.Status == "PendingPayment"));
			return result;
		}

		public async Task<int> CountActiveByZoneAsync(string zoneId)
		{
			var list = await GetAllAsync();
			// Active means occupied slot: Active (parked) or PendingPayment (waiting to exit)
			return list.Count(s => (s.Status == "Active" || s.Status == "PendingPayment") && s.ParkingZoneId == zoneId);
		}

		public async Task<ParkingSession?> FindByTicketIdAsync(string ticketId)
		{
			var list = await GetAllAsync();
			return list.FirstOrDefault(s => s.Ticket?.TicketId == ticketId);
		}

		public async Task<ParkingSession?> GetActiveSessionByCardIdAsync(string cardId)
		{
			if (string.IsNullOrEmpty(cardId)) return null;
			var list = await GetAllAsync();
			return list.FirstOrDefault(s => 
				(s.Status == "Active" || s.Status == "PendingPayment") && 
				(s.CardId == cardId || s.Ticket?.CardId == cardId));
		}
	}
}
