using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

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

        public ReportController(
            IParkingSessionRepository sessionRepo,
            IIncidentRepository incidentRepo,
            IMonthlyTicketRepository monthlyTicketRepo,
            IMembershipHistoryRepository membershipHistoryRepo)
        {
            _sessionRepo = sessionRepo;
            _incidentRepo = incidentRepo;
            _monthlyTicketRepo = monthlyTicketRepo;
            _membershipHistoryRepo = membershipHistoryRepo;
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

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1);

            // 1. Session Revenue
            var allSessions = await _sessionRepo.GetAllAsync();
            var completedSessions = allSessions
                .Where(s => s.Status == "Completed" && s.Payment != null && s.Payment.Time >= start && s.Payment.Time <= end)
                .ToList();

            // 2. Incident Fines (Lost Ticket) - Assuming fines are part of Incident resolution or stored separately. 
            // For now, let's assume lost ticket fees are collected via normal sessions (ticket lost fee added to amount).
            // Or if we want to track specifically lost tickets from IncidentRepo:
            // This example assumes revenue is primarily from Sessions. 
            // If Membership fees are separate, we add them here.

            // 3. Membership Revenue (History)
            // (Assumes MembershipHistory has 'Time' and 'Amount')
            // Need to implement GetAllAsync in generic repo or specific query
            // Since BaseJsonRepository has GetAllAsync, we can cast if exposed, or just rely on what we have.
            // Let's assume for now we only count Session revenue in this endpoint, 
            // OR we combine both. Let's combine both for "Total Revenue".

            // Note: Use a dedicated method in Repo for performance in real DB, here InMemory/JSON is fine.
            // We need access to all history. The Interface doesn't expose GetAll, let's rely on specific repositories or assume we add GetAll to interface if needed.
            // For now, let's stick to Parking Revenue.

            var report = new RevenueReport
            {
                StartDate = start,
                EndDate = end,
                TotalTransactions = completedSessions.Count,
                TotalRevenue = completedSessions.Sum(s => s.Payment.Amount),
                RevenueByPaymentMethod = completedSessions
                    .GroupBy(s => s.Payment.Method ?? "Unknown") // Default to Unknown (since Cash is not allowed)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Payment.Amount))
            };

            return Ok(report);
        }

        [HttpGet("lost-tickets")]
        public async Task<IActionResult> GetLostTickets([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1);

            // Get incidents with Type = "LostTicket"
            // Filter locally since Repo currently only has FindOpenIncidents
            // Ideally add FindByDateRange to Repo
            var allIncidents = await _incidentRepo.GetAllAsync();
            var lostTicketIncidents = allIncidents
                .Where(i => i.ReportedDate >= start && i.ReportedDate <= end && (i.Type == "LostTicket" || i.Title.Contains("Mất vé")))
                .ToList();

            return Ok(new
            {
                StartDate = start,
                EndDate = end,
                Count = lostTicketIncidents.Count,
                List = lostTicketIncidents.Select(i => new { i.ReportedDate, i.Title, i.Status })
            });
        }

        [HttpGet("membership")]
        public async Task<IActionResult> GetMembershipStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            // Simple stats: Active tickets count
            // New registrations in range (Need History or CreatedDate on Ticket)
            // For this version, let's just return Active Monthly Tickets Count and Expiring Soon
            
            var allTickets = await _monthlyTicketRepo.GetAllAsync();
            var activeCount = allTickets.Count(t => t.IsValid());
            var expiringSoon = allTickets.Count(t => t.IsValid() && t.ExpiryDate <= DateTime.Now.AddDays(7));

            return Ok(new
            {
                ActiveCount = activeCount,
                ExpiringSoon = expiringSoon
            });
        }

        [HttpGet("traffic")]
        public async Task<IActionResult> GetTrafficReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today.AddDays(1).AddTicks(-1);

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

            var report = new TrafficReport
            {
                StartDate = start,
                EndDate = end,
                TotalVehiclesIn = vehiclesIn.Count,
                TotalVehiclesOut = vehiclesOut.Count,
                VehiclesByType = vehiclesIn
                    .GroupBy(s => s.Vehicle.GetType().Name) // Or use VehicleType string if available
                    .ToDictionary(g => g.Key, g => g.Count()),
                HourlyTraffic = hourlyStats
            };

            return Ok(report);
        }
        
        public class HourlyTrafficParam
        {
            public string Hour { get; set; }
            public int Entries { get; set; }
            public int Exits { get; set; }
        }
    }
}
