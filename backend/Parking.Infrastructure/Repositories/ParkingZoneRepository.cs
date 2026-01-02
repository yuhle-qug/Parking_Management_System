using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
	public class ParkingZoneRepository : IParkingZoneRepository
	{
		// Seed with a few zones so the demo works without external data.
		private static readonly List<ParkingZone> Zones = new()
		{
			new ParkingZone { ZoneId = "Z1", Name = "Car Zone", VehicleCategory = "CAR", ElectricOnly = false, Capacity = 100, PricePolicy = new ParkingFeePolicy { PolicyId = "P1", Name = "Car Standard" } },
			new ParkingZone { ZoneId = "Z2", Name = "Bike Zone", VehicleCategory = "MOTORBIKE", ElectricOnly = false, Capacity = 200, PricePolicy = new ParkingFeePolicy { PolicyId = "P2", Name = "Bike Standard" } },
			new ParkingZone { ZoneId = "Z3", Name = "EV Car Zone", VehicleCategory = "CAR", ElectricOnly = true, Capacity = 50, PricePolicy = new ParkingFeePolicy { PolicyId = "P3", Name = "EV Discount" } },
		};

		public Task AddAsync(ParkingZone entity)
		{
			Zones.Add(entity);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<ParkingZone>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<ParkingZone>>(Zones);
		}

		public Task<ParkingZone?> GetByIdAsync(string id)
		{
			var zone = Zones.FirstOrDefault(z => z.ZoneId == id);
			return Task.FromResult(zone);
		}

		public Task UpdateAsync(ParkingZone entity)
		{
			var index = Zones.FindIndex(z => z.ZoneId == entity.ZoneId);
			if (index >= 0)
			{
				Zones[index] = entity;
			}
			return Task.CompletedTask;
		}

		public Task<ParkingZone?> FindSuitableZoneAsync(string vehicleType, bool isElectric)
		{
			var zone = Zones.FirstOrDefault(z =>
				z.VehicleCategory.ToUpper() == vehicleType.ToUpper() &&
				(!z.ElectricOnly || isElectric) &&
				!z.IsFull());

			return Task.FromResult(zone);
		}
	}
}
