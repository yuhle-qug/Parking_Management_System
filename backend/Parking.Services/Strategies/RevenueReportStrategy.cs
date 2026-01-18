using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Interfaces;

namespace Parking.Services.Strategies
{
    public class RevenueReportStrategy : IReportStrategy
    {
        private readonly IParkingSessionRepository _sessionRepo;

        public RevenueReportStrategy(IParkingSessionRepository sessionRepo)
        {
            _sessionRepo = sessionRepo;
        }

        public async Task<object> GenerateReportAsync(DateTime from, DateTime to)
        {
            DateTime start = from;
            DateTime end = to.AddDays(1).AddTicks(-1); // Ensure end of day if only date provided, or trust input. 
            // Controller logic: DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1); 
            // But interface takes non-nullable DateTime. We assume 'to' is passed correctly or we adjust.
            // Let's assume 'to' is the inclusive end date from the UI (e.g. 2023-10-27). 
            // If the UI sends 2023-10-27 00:00:00, we want to include the whole day.
            // The Controller logic was: DateTime end = to ?? ...
            // Let's replicate the safe end-of-day logic if the time is 00:00:00.
            
            if (end.Hour == 0 && end.Minute == 0 && end.Second == 0)
            {
                end = end.AddDays(1).AddTicks(-1);
            }

            var allSessions = await _sessionRepo.GetAllAsync();
            var completedSessions = allSessions
                .Where(s => s.Status == "Completed" && s.Payment != null && s.Payment.Time >= start && s.Payment.Time <= end)
                .ToList();

            var report = new RevenueReport
            {
                StartDate = start,
                EndDate = end,
                TotalTransactions = completedSessions.Count,
                TotalRevenue = completedSessions.Sum(s => s.Payment?.Amount ?? 0),
                RevenueByPaymentMethod = completedSessions
                    .GroupBy(s => s.Payment?.Method ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Payment?.Amount ?? 0)),
                Summary = CalculateSummary(allSessions)
            };

            return report;
        }

        private RevenueSummary CalculateSummary(IEnumerable<ParkingSession> allSessions)
        {
            var completed = allSessions.Where(s => s.Status == "Completed" && s.Payment != null).ToList();
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
            if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = today.AddDays(-6);
             // Note: DayOfWeek.Monday is 1. If today is Sunday (0), -(0) + 1 = +1 (Tomorrow?) No.
             // If Sunday, we want last Monday. 
             // Logic in Controller was: today.AddDays(-(int)today.DayOfWeek + 1)
             // Sunday(0) -> +1 (Monday next week? No, works if we treat Sunday as 7). 
             // C# DayOfWeek: Sunday=0, Monday=1...Saturday=6.
             // If Sunday(0): AddDays(1) -> Monday. 
             // Correct logic for "Monday is start":
             // diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7
             // start = today.AddDays(-diff).Date;

            double SumRevenue(DateTime from) 
                => completed.Where(s => s.Payment.Time >= from).Sum(s => s.Payment.Amount);

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            
            // Re-eval StartOfWeek correctly
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            startOfWeek = today.AddDays(-1 * diff).Date;

            return new RevenueSummary
            {
                Today = SumRevenue(today),
                ThisWeek = SumRevenue(startOfWeek),
                ThisMonth = SumRevenue(startOfMonth),
                ThisYear = SumRevenue(startOfYear)
            };
        }
    }
}
