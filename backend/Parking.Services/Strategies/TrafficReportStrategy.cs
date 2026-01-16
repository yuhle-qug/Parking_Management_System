using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Interfaces;

namespace Parking.Services.Strategies
{
    public class TrafficReportStrategy : IReportStrategy
    {
        private readonly IParkingSessionRepository _sessionRepo;

        public TrafficReportStrategy(IParkingSessionRepository sessionRepo)
        {
            _sessionRepo = sessionRepo;
        }

        public async Task<object> GenerateReportAsync(DateTime from, DateTime to)
        {
            DateTime start = from;
            DateTime end = to;

            if (end.Hour == 0 && end.Minute == 0 && end.Second == 0)
            {
                end = end.AddDays(1).AddTicks(-1);
            }

            var allSessions = await _sessionRepo.GetAllAsync();

            var vehiclesIn = allSessions
                .Where(s => s.EntryTime >= start && s.EntryTime <= end)
                .ToList();

            var vehiclesOut = allSessions
                .Where(s => s.ExitTime.HasValue && s.ExitTime >= start && s.ExitTime <= end)
                .ToList();

            var hourlyStats = new List<HourlyTrafficParam>();
            for (int i = 0; i < 24; i++)
            {
                hourlyStats.Add(new HourlyTrafficParam 
                { 
                    Hour = $"{i}h", 
                    Entries = vehiclesIn.Count(s => s.EntryTime.Hour == i),
                    Exits = vehiclesOut.Count(s => s.ExitTime.HasValue && s.ExitTime.Value.Hour == i) 
                });
            }

            var rawStats = vehiclesIn
                    .Where(s => s.Vehicle != null) // Safety check
                    .GroupBy(s => s.Vehicle.GetType().Name) 
                    .ToDictionary(g => g.Key, g => g.Count());

            var allTypes = new[] { "Car", "ElectricCar", "Motorbike", "ElectricMotorbike", "Bicycle" };
            var vehicleStats = new Dictionary<string, int>();
            
            foreach (var type in allTypes)
            {
                vehicleStats[type] = rawStats.ContainsKey(type) ? rawStats[type] : 0;
            }

            var report = new TrafficReport
            {
                StartDate = start,
                EndDate = end,
                TotalVehiclesIn = vehiclesIn.Count,
                TotalVehiclesOut = vehiclesOut.Count,
                VehiclesByType = vehicleStats,
                HourlyTraffic = hourlyStats
            };

            return report;
        }

        // Helper class for Hourly Traffic (Internal to this strategy or reused if shared)
        // Since it was defined inside Controller, we redefine it here or move it to Core.
        // For loose coupling, let's redefine it here or use anonymous object if Output is object.
        // But TrafficReport entity uses 'object HourlyTraffic', so we can pass List<HourlyTrafficParam>.
        public class HourlyTrafficParam
        {
            public string Hour { get; set; }
            public int Entries { get; set; }
            public int Exits { get; set; }
        }
    }
}
