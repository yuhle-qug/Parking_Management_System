using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Interfaces;

namespace Parking.Services.Strategies
{
    public class RevenueChartStrategy : IReportStrategy
    {
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IMembershipHistoryRepository _historyRepo;

        public RevenueChartStrategy(IParkingSessionRepository sessionRepo, IMembershipHistoryRepository historyRepo)
        {
            _sessionRepo = sessionRepo;
            _historyRepo = historyRepo;
        }

        public async Task<object> GenerateReportAsync(DateTime from, DateTime to)
        {
            // Logic: Determine grouping based on duration
            // Frontend sends 'from' and 'to' covering the view range.
            var duration = (to - from).TotalDays;
            bool isDaily = duration <= 60; // Heuristic: <= 2 months -> shows days

            var sessions = await _sessionRepo.GetAllAsync();
            var history = await _historyRepo.GetAllAsync();

            var chartData = new List<ChartDataPoint>();
            DateTime current = from.Date;
            DateTime endFinal = to.Date.AddDays(1).AddTicks(-1);

            if (isDaily)
            {
                // Group by Day
                while (current <= to.Date)
                {
                    var dayEnd = current.AddDays(1).AddTicks(-1);
                    
                    var sessionRev = (decimal)sessions
                        .Where(s => s.Status == "Completed" && s.Payment?.Time >= current && s.Payment?.Time <= dayEnd)
                        .Sum(s => s.Payment?.Amount ?? 0);

                    var membershipRev = (decimal)history
                        .Where(h => h.Time >= current && h.Time <= dayEnd && (h.Action == "Register" || h.Action == "Extend"))
                        .Sum(h => h.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = current.ToString("dd/MM"),
                        Value = sessionRev + membershipRev,
                        Date = current
                    });

                    current = current.AddDays(1);
                }
            }
            else
            {
                // Group by Month
                // Snap 'current' to start of month
                current = new DateTime(from.Year, from.Month, 1);
                
                while (current <= to.Date)
                {
                    var monthEnd = current.AddMonths(1).AddTicks(-1);
                    var searchEnd = monthEnd > endFinal ? endFinal : monthEnd; // Bound by requested 'to' if needed

                    var sessionRev = (decimal)sessions
                        .Where(s => s.Status == "Completed" && s.Payment?.Time >= current && s.Payment?.Time <= searchEnd)
                        .Sum(s => s.Payment?.Amount ?? 0);

                    var membershipRev = (decimal)history
                        .Where(h => h.Time >= current && h.Time <= searchEnd && (h.Action == "Register" || h.Action == "Extend"))
                        .Sum(h => h.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = $"T{current.Month}/{current.ToString("yy")}",
                        Value = sessionRev + membershipRev,
                        Date = current
                    });

                    current = current.AddMonths(1);
                }
            }

            return chartData;
        }
    }
}
