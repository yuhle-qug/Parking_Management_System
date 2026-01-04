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
		// Gọi constructor của lớp cha để định nghĩa tên file là "zones.json"
		public ParkingZoneRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "zones.json")
		{
			// Tự động tạo dữ liệu mẫu ngay khi khởi động nếu file chưa có
			// .Wait() được dùng ở đây để đảm bảo data có sẵn trước khi app chạy tiếp
			SeedDataAsync().Wait();
		}

		private async Task SeedDataAsync()
		{
			// Kiểm tra xem file đã có dữ liệu chưa
			var zones = await GetAllAsync();
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
						PricePolicy = new ParkingFeePolicy { PolicyId = "P-CAR", Name = "Phí ô tô tiêu chuẩn" }
					},
					new ParkingZone {
						ZoneId = "ZONE-B",
						Name = "Khu B (Xe máy)",
						Capacity = 100,
						VehicleCategory = "MOTORBIKE",
						PricePolicy = new ParkingFeePolicy { PolicyId = "P-MOTO", Name = "Phí xe máy tiêu chuẩn" }
					},
					new ParkingZone {
						ZoneId = "ZONE-E",
						Name = "Khu E (Xe điện)",
						Capacity = 20,
						VehicleCategory = "CAR",
						ElectricOnly = true,
						PricePolicy = new ParkingFeePolicy { PolicyId = "P-ELEC", Name = "Phí xe điện ưu đãi" }
					}
				};

				// Lưu xuống file json
				await JsonFileHelper.WriteListAsync(_filePath, seedData);
			}
		}

		public async Task<ParkingZone?> FindSuitableZoneAsync(string vehicleType, bool isElectric)
		{
			var zones = await GetAllAsync();
			var vt = (vehicleType ?? string.Empty).Trim().ToUpperInvariant();

			// 1) Nếu là xe điện -> ưu tiên khu ElectricOnly (nhưng vẫn phải khớp category) và còn chỗ
			if (isElectric)
			{
				var electricZone = zones.FirstOrDefault(z =>
					z != null &&
					z.ElectricOnly &&
					!z.IsFull() &&
					IsCategoryMatch(z.VehicleCategory, vt));
				if (electricZone != null) return electricZone;
			}

			// 2) Fallback: khu thường khớp loại xe và còn chỗ
			return zones.FirstOrDefault(z =>
				z != null &&
				!z.ElectricOnly &&
				!z.IsFull() &&
				IsCategoryMatch(z.VehicleCategory, vt));
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

			return false;
		}
	}
}
