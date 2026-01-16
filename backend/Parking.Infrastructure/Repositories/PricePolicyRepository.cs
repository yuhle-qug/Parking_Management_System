using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories
{
    public class PricePolicyRepository : BaseJsonRepository<PricePolicy>, IPricePolicyRepository
    {
        public PricePolicyRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "price_policies.json")
        {
            SeedDataAsync().GetAwaiter().GetResult();
        }

        private async Task SeedDataAsync()
        {
            var list = (await GetAllAsync()).ToList();
            if (list.Any()) return;

            var seed = new List<PricePolicy>
            {
                new PricePolicy
                {
                    PolicyId = "P-CAR",
                    Name = "Phí ô tô tiêu chuẩn",
                    VehicleType = "CAR",
                    RatePerHour = 12000,
                    OvernightSurcharge = 30000,
                    DailyMax = 150000,
                    LostTicketFee = 200000,
                    PeakRanges = new List<PeakRange> { new PeakRange { StartHour = 17, EndHour = 21, Multiplier = 1.5 } }
                },
                new PricePolicy
                {
                    PolicyId = "P-MOTO",
                    Name = "Phí xe máy tiêu chuẩn",
                    VehicleType = "MOTORBIKE",
                    RatePerHour = 4000,
                    OvernightSurcharge = 10000,
                    DailyMax = 50000,
                    LostTicketFee = 50000,
                    PeakRanges = new List<PeakRange> { new PeakRange { StartHour = 17, EndHour = 21, Multiplier = 1.3 } }
                },
                new PricePolicy
                {
                    PolicyId = "P-BIKE",
                    Name = "Phí xe đạp",
                    VehicleType = "BICYCLE",
                    RatePerHour = 2000,
                    OvernightSurcharge = 5000,
                    DailyMax = 20000,
                    LostTicketFee = 20000
                },
                new PricePolicy
                {
                    PolicyId = "P-ELEC",
                    Name = "Phí ô tô điện ưu đãi",
                    VehicleType = "ELECTRIC_CAR",
                    RatePerHour = 8000,
                    OvernightSurcharge = 20000,
                    DailyMax = 100000,
                    LostTicketFee = 200000
                },
                new PricePolicy
                {
                    PolicyId = "P-ELEC-MOTO",
                    Name = "Phí xe máy điện",
                    VehicleType = "ELECTRIC_MOTORBIKE",
                    RatePerHour = 3000,
                    OvernightSurcharge = 8000,
                    DailyMax = 40000,
                    LostTicketFee = 40000
                }
            };

            await JsonFileHelper.WriteListAsync(_filePath, seed);
        }

        public async Task<PricePolicy?> GetPolicyAsync(string policyId)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(p => p.PolicyId.Equals(policyId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<PricePolicy?> GetPolicyByVehicleTypeAsync(string vehicleType)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(p => p.VehicleType.Equals(vehicleType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
