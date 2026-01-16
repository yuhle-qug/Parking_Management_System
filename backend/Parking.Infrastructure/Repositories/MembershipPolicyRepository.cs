using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories
{
    public class MembershipPolicyRepository : BaseJsonRepository<MembershipPolicy>, IMembershipPolicyRepository
    {
        public MembershipPolicyRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "membership_policies.json")
        {
            SeedDataAsync().GetAwaiter().GetResult();
        }

        private async Task SeedDataAsync()
        {
            var list = (await GetAllAsync()).ToList();
            if (!list.Any())
            {
                var seed = new List<MembershipPolicy>
                {
                    new MembershipPolicy { PolicyId = "P-CAR", Name = "Monthly Car", VehicleType = "CAR", MonthlyPrice = 1_500_000 },
                    new MembershipPolicy { PolicyId = "P-MOTO", Name = "Monthly Motorbike", VehicleType = "MOTORBIKE", MonthlyPrice = 120_000 },
                    new MembershipPolicy { PolicyId = "P-ELEC", Name = "Monthly Electric Car", VehicleType = "ELECTRIC_CAR", MonthlyPrice = 1_000_000 },
                    new MembershipPolicy { PolicyId = "P-BIKE", Name = "Monthly Bicycle", VehicleType = "BICYCLE", MonthlyPrice = 80_000 }
                };

                await JsonFileHelper.WriteListAsync(_filePath, seed);
            }
            else if (!list.Any(p => string.Equals(p.VehicleType, "BICYCLE", System.StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(new MembershipPolicy { PolicyId = "P-BIKE", Name = "Monthly Bicycle", VehicleType = "BICYCLE", MonthlyPrice = 80_000 });
                await JsonFileHelper.WriteListAsync(_filePath, list);
            }
        }

        public async Task<MembershipPolicy?> GetPolicyAsync(string vehicleType)
        {
            var policies = await GetAllAsync();
            return policies.FirstOrDefault(p => p.VehicleType.Equals(vehicleType, System.StringComparison.OrdinalIgnoreCase))
                   ?? policies.FirstOrDefault(p => p.VehicleType == "CAR");
        }
    }
}
