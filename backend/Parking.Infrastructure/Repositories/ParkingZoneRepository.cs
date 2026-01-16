using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Infrastructure.Data; // Để dùng JsonFileHelper

namespace Parking.Infrastructure.Repositories
{
	public class ParkingZoneRepository : BaseJsonRepository<ParkingZone>, IParkingZoneRepository
	{
		private readonly IParkingSessionRepository _sessionRepo;
		// Gọi constructor của lớp cha để định nghĩa tên file là "zones.json"
		public ParkingZoneRepository(IHostEnvironment hostEnvironment, IParkingSessionRepository sessionRepo) : base(hostEnvironment, "zones.json")
		{
			_sessionRepo = sessionRepo;
			// Tự động tạo dữ liệu mẫu ngay khi khởi động nếu file chưa có
			// .Wait() được dùng ở đây để đảm bảo data có sẵn trước khi app chạy tiếp
			SeedDataAsync().Wait();
		}

		private async Task SeedDataAsync()
		{
			// Kiểm tra xem file đã có dữ liệu chưa
			var zones = (await GetAllAsync()).ToList();
			if (!zones.Any())
			{
				// Nếu chưa có, tạo bộ dữ liệu mặc định (Seed Data)
				var seedData = new List<ParkingZone>
				{
					new ParkingZone {
						ZoneId = "ZONE-A",
						Name = "Khu A (Ô tô)",
						Capacity = 50,
						VehicleCategory = "CAR",
						GateIds = new List<string> { "GATE-IN-01", "GATE-IN-02" },
						PricePolicyId = "P-CAR"
					},
					new ParkingZone {
						ZoneId = "ZONE-B",
						Name = "Khu B (Xe máy)",
						Capacity = 100,
						VehicleCategory = "MOTORBIKE",
						GateIds = new List<string> { "GATE-IN-01", "GATE-IN-03" },
						PricePolicyId = "P-MOTO"
					},
					new ParkingZone {
						ZoneId = "ZONE-C",
						Name = "Khu C (Xe đạp)",
						Capacity = 80,
						VehicleCategory = "BICYCLE",
						GateIds = new List<string> { "GATE-IN-02", "GATE-IN-03" },
						PricePolicyId = "P-BIKE"
					},
					new ParkingZone {
						ZoneId = "ZONE-E",
						Name = "Khu E (Xe điện)",
						Capacity = 20,
						VehicleCategory = "CAR",
						ElectricOnly = true,
						GateIds = new List<string> { "GATE-IN-02" },
						PricePolicyId = "P-ELEC"
					},
					new ParkingZone {
						ZoneId = "ZONE-EM",
						Name = "Khu EM (Xe máy điện)",
						Capacity = 40,
						VehicleCategory = "MOTORBIKE",
						ElectricOnly = true,
						GateIds = new List<string> { "GATE-IN-01", "GATE-IN-03" },
						PricePolicyId = "P-ELEC-MOTO"
					}
				};

				// Lưu xuống file json
				await JsonFileHelper.WriteListAsync(_filePath, seedData);
			}
			else if (!zones.Any(z => string.Equals(z.VehicleCategory, "BICYCLE", System.StringComparison.OrdinalIgnoreCase)))
			{
				zones.Add(new ParkingZone
				{
					ZoneId = "ZONE-C",
					Name = "Khu C (Xe đạp)",
					Capacity = 80,
					VehicleCategory = "BICYCLE",
					GateIds = new List<string> { "GATE-IN-02", "GATE-IN-03" },
					PricePolicyId = "P-BIKE"
				});
				await JsonFileHelper.WriteListAsync(_filePath, zones.ToList());
			}
		}

		public async Task<ParkingZone?> FindSuitableZoneAsync(string vehicleType, bool isElectric, string gateId)
		{
			var zones = await GetAllAsync();
			var vt = (vehicleType ?? string.Empty).Trim().ToUpperInvariant();
			var gate = (gateId ?? string.Empty).Trim().ToUpperInvariant();

			bool IsGateMatch(ParkingZone z) =>
				z.GateIds == null || !z.GateIds.Any() || z.GateIds.Any(g => string.Equals(g, gate, System.StringComparison.OrdinalIgnoreCase));

			async Task<bool> HasCapacityAsync(ParkingZone z)
			{
				var active = await _sessionRepo.CountActiveByZoneAsync(z.ZoneId);
				return active < z.Capacity;
			}

			// 1) Nếu là xe điện -> ưu tiên khu ElectricOnly đúng gate và còn chỗ
			if (isElectric)
			{
				foreach (var z in zones)
				{
					if (z == null || !z.ElectricOnly || !IsCategoryMatch(z.VehicleCategory, vt) || !IsGateMatch(z)) continue;
					if (await HasCapacityAsync(z)) return z;
				}
			}

			// 2) Fallback: khu thường khớp loại xe, đúng gate và còn chỗ
			foreach (var z in zones)
			{
				if (z == null || z.ElectricOnly || !IsCategoryMatch(z.VehicleCategory, vt) || !IsGateMatch(z)) continue;
				if (await HasCapacityAsync(z)) return z;
			}

			// 3) Cuối cùng: nếu gate này hết chỗ, thử các gate khác (để tránh chặn hoàn toàn)
			foreach (var z in zones)
			{
				if (z == null || !IsCategoryMatch(z.VehicleCategory, vt)) continue;
				if (await HasCapacityAsync(z)) return z;
			}

			return null;
		}

		// Helper check loại xe
		private bool IsCategoryMatch(string zoneCat, string vehicleType)
		{
			var cat = (zoneCat ?? string.Empty).ToUpperInvariant();
			var vt = (vehicleType ?? string.Empty).ToUpperInvariant();

			// Ví dụ: Zone là CAR thì nhận CAR, ELECTRIC_CAR, TRUCK...
			if (cat == "CAR" && vt.Contains("CAR")) return true;

			// Ví dụ: Zone là MOTORBIKE thì nhận MOTORBIKE, ELECTRIC_MOTORBIKE...
			if (cat == "MOTORBIKE" && vt.Contains("MOTORBIKE")) return true;

			if (cat == "BICYCLE" && vt.Contains("BICYCLE")) return true;

			return false;
		}
	}
}
