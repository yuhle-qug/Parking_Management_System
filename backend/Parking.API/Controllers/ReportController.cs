using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Interfaces; // Add this
using Parking.Services.Strategies; // Add this

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IIncidentRepository _incidentRepo;
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;
        private readonly IMembershipHistoryRepository _membershipHistoryRepo;
        private readonly IReportFactory _reportFactory;

        public ReportController(
            IParkingSessionRepository sessionRepo,
            IIncidentRepository incidentRepo,
            IMonthlyTicketRepository monthlyTicketRepo,
            IMembershipHistoryRepository membershipHistoryRepo,
            IReportFactory reportFactory)
        {
            _sessionRepo = sessionRepo;
            _incidentRepo = incidentRepo;
            _monthlyTicketRepo = monthlyTicketRepo;
            _membershipHistoryRepo = membershipHistoryRepo;
            _reportFactory = reportFactory;
        }

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var allSessions = await _sessionRepo.GetAllAsync();
            var activeSessions = allSessions
                .Where(s => s.Status != "Completed")
                .OrderByDescending(s => s.EntryTime)
                .ToList();

            return Ok(activeSessions);
        }

        // [REFACTORED] Generic Endpoint using Strategy Pattern
        // Replaces old /revenue and /traffic endpoints
        [HttpGet]
        public async Task<IActionResult> RequestReport(
            [FromQuery] string type, 
            [FromQuery] DateTime? from, 
            [FromQuery] DateTime? to)
        {
            if (string.IsNullOrEmpty(type))
                return BadRequest("Report type is required");

            // Default range if not provided (aligned with previous logic)
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1);

            try 
            {
                var strategy = _reportFactory.GetStrategy(type);
                var data = await strategy.GenerateReportAsync(start, end);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        // --- LEGACY/SPECIFIC ENDPOINTS (To be refactored later) ---

        // [New] GET: api/Report/revenue/summary
        [HttpGet("revenue/summary")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var allSessions = await _sessionRepo.GetAllAsync();
            var completed = allSessions.Where(s => s.Status == "Completed" && s.Payment != null).ToList();

            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            double SumRevenue(DateTime from) 
                => completed.Where(s => s.Payment.Time >= from).Sum(s => s.Payment.Amount);

            return Ok(new
            {
                Today = SumRevenue(today),
                ThisWeek = SumRevenue(startOfWeek),
                ThisMonth = SumRevenue(startOfMonth),
                ThisYear = SumRevenue(startOfYear)
            });
        }

        [HttpGet("lost-tickets")]
        public async Task<IActionResult> GetLostTickets([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1);

            // 1. Incidents
            var allIncidents = await _incidentRepo.GetAllAsync();
            var lostTicketIncidents = allIncidents
                .Where(i => i.ReportedDate >= start && i.ReportedDate <= end && 
                           (i.Type == "LostTicket" || 
                            (i.Title != null && (i.Title.Contains("Mất vé") || i.Title.Contains("Lost Ticket")))))
                .ToList();

            // 2. Lost Ticket Sessions
            var allSessions = await _sessionRepo.GetAllAsync();
            var lostSessions = allSessions
                .Where(s => (s.Status == "LostTicket" || s.Ticket?.TicketType == "Lost") && s.EntryTime >= start && s.EntryTime <= end)
                .ToList();

            var combinedCount = lostTicketIncidents.Count + lostSessions.Count;

            // Simple Merge (Optional: detailed list merge)
            var displayList = lostTicketIncidents.Select(i => new { Date = i.ReportedDate, Title = i.Title, Status = i.Status })
                .Concat(lostSessions.Select(s => new { Date = s.EntryTime, Title = $"Mất vé xe {s.Vehicle.LicensePlate.Value}", Status = "Processing" }))
                .OrderByDescending(x => x.Date)
                .ToList();

            return Ok(new
            {
                StartDate = start,
                EndDate = end,
                Count = combinedCount,
                List = displayList
            });
        }

        [HttpGet("membership")]
        public async Task<IActionResult> GetMembershipStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var allTickets = await _monthlyTicketRepo.GetAllAsync();
            var activeCount = allTickets.Count(t => t.IsValid());
            var expiringSoon = allTickets.Count(t => t.IsValid() && t.ExpiryDate <= DateTime.Now.AddDays(7));

            return Ok(new
            {
                ActiveCount = activeCount,
                ExpiringSoon = expiringSoon
            });
        }

        // WARNING: hourlyStats/HourlyTrafficParam helper class was removed from Controller body.
        // It's used in 'chart' endpoint logic? No, chart is different.
        // But TrafficReportStrategy now handles 'traffic'.
        // So GetTrafficReport is GONE.
        // However, I need to check if GetRevenueChart uses HourlyTrafficParam. No, it uses ChartDataPoint.


        
        public class HourlyTrafficParam
        {
            public string Hour { get; set; }
            public int Entries { get; set; }
            public int Exits { get; set; }
        }
        [HttpGet("revenue/chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] string type = "week", [FromQuery] DateTime? date = null)
        {
            var sessions = await _sessionRepo.GetAllAsync();
            var membershipHistory = await _membershipHistoryRepo.GetAllAsync();

            var anchorDate = date ?? DateTime.Now;
            var chartData = new List<ChartDataPoint>();

            if (type.ToLower() == "week") 
            {
                // Logic: Mon-Sun of the ANCHOR week
                var startOfWeek = anchorDate.AddDays(-(int)anchorDate.DayOfWeek + 1).Date; // Mon
                if (anchorDate.DayOfWeek == DayOfWeek.Sunday) startOfWeek = anchorDate.AddDays(-6).Date;

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = startOfWeek.AddDays(i);
                    var endOfDay = currentDate.AddDays(1).AddTicks(-1);

                    var sessionRev = (decimal)sessions
                        .Where(s => s.Status == "Completed" && s.Payment?.Time >= currentDate && s.Payment?.Time <= endOfDay)
                        .Sum(s => s.Payment?.Amount ?? 0);

                    var membershipRev = (decimal)membershipHistory
                        .Where(h => h.Time >= currentDate && h.Time <= endOfDay && (h.Action == "Register" || h.Action == "Extend"))
                        .Sum(h => h.Amount);

                    chartData.Add(new ChartDataPoint 
                    { 
                        Label = currentDate.ToString("dd/MM"), // e.g. 27/01
                        Value = sessionRev + membershipRev,
                        Date = currentDate
                    });
                }
            }
            else // "year" -> 12 months of the ANCHOR year
            {
                var startOfYear = new DateTime(anchorDate.Year, 1, 1);
                
                for (int m = 1; m <= 12; m++)
                {
                    var startOfMonth = new DateTime(anchorDate.Year, m, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                    var sessionRev = (decimal)sessions
                        .Where(s => s.Status == "Completed" && s.Payment?.Time >= startOfMonth && s.Payment?.Time <= endOfMonth)
                        .Sum(s => s.Payment?.Amount ?? 0);

                    var membershipRev = (decimal)membershipHistory
                        .Where(h => h.Time >= startOfMonth && h.Time <= endOfMonth && (h.Action == "Register" || h.Action == "Extend"))
                        .Sum(h => h.Amount);

                    chartData.Add(new ChartDataPoint
                    {
                        Label = $"T{m}",
                        Value = sessionRev + membershipRev,
                        Date = startOfMonth
                    });
                }
            }

            return Ok(chartData);
        }

        public class ChartDataPoint
        {
            public string Label { get; set; }
            public decimal Value { get; set; }
            public DateTime Date { get; set; }
        }
    }
}
