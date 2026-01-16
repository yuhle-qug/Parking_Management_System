using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories
{
    public class GateRepository : BaseJsonRepository<Gate>, IGateRepository
    {
        public GateRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "gates.json")
        {
            SeedDataAsync().Wait();
        }

        private async Task SeedDataAsync()
        {
            var gates = (await GetAllAsync()).ToList();
            if (gates.Any()) return;

            var seedData = new List<Gate>
            {
                // -- ENTRANCE GATES (CAR LANE) --
                // Cổng 1 & 2 cho Ô tô (Thường + Điện)
                new Gate { 
                    GateId = "GATE-IN-CAR-01", 
                    Name = "Cổng Ôtô 01", 
                    Direction = "In", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "CAR", "ELECTRIC_CAR" } 
                },
                new Gate { 
                    GateId = "GATE-IN-CAR-02", 
                    Name = "Cổng Ôtô 02", 
                    Direction = "In", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "CAR", "ELECTRIC_CAR" } 
                },

                // -- ENTRANCE GATES (MOTO LANE) --
                // Cổng 1, 2, 3 cho Xe máy (Thường + Điện) + Xe đạp
                new Gate { 
                    GateId = "GATE-IN-MOTO-01", 
                    Name = "Cổng Xe máy 01", 
                    Direction = "In", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                },
                new Gate { 
                    GateId = "GATE-IN-MOTO-02", 
                    Name = "Cổng Xe máy 02", 
                    Direction = "In", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                },
                new Gate { 
                    GateId = "GATE-IN-MOTO-03", 
                    Name = "Cổng Xe máy 03", 
                    Direction = "In", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                },
                
                // -- EXIT GATES (CAR LANE) --
                new Gate { 
                    GateId = "GATE-OUT-CAR-01", 
                    Name = "Cổng ra Ô tô 01", 
                    Direction = "Out", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "CAR", "ELECTRIC_CAR" } 
                },
                new Gate { 
                    GateId = "GATE-OUT-CAR-02", 
                    Name = "Cổng ra Ô tô 02", 
                    Direction = "Out", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "CAR", "ELECTRIC_CAR" } 
                },

                // -- EXIT GATES (MOTO LANE) --
                new Gate { 
                    GateId = "GATE-OUT-MOTO-01", 
                    Name = "Cổng ra Xe máy 01", 
                    Direction = "Out", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                },
                new Gate { 
                    GateId = "GATE-OUT-MOTO-02", 
                    Name = "Cổng ra Xe máy 02", 
                    Direction = "Out", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                },
                new Gate { 
                    GateId = "GATE-OUT-MOTO-03", 
                    Name = "Cổng ra Xe máy 03", 
                    Direction = "Out", 
                    Status = "Active",
                    AllowedVehicleCategories = new List<string> { "MOTORBIKE", "ELECTRIC_MOTORBIKE", "BICYCLE" } 
                }
            };

            await JsonFileHelper.WriteListAsync(_filePath, seedData);
        }

        public async Task<IEnumerable<Gate>> GetGatesForVehicleAsync(string vehicleCategory)
        {
            var all = await GetAllAsync();
            var cat = vehicleCategory?.ToUpper();
            return all.Where(g => g.AllowedVehicleCategories.Contains(cat));
        }
    }
}
