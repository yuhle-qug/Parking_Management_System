using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Interfaces; // Add this
using Parking.Services.Strategies; // Add this

using Microsoft.AspNetCore.Authorization;

namespace Parking.API.Controllers
{
    [Authorize(Roles = "ADMIN, ATTENDANT")]
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
        [Authorize(Roles = "ADMIN")]
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

        // --- LEGACY/SPECIFIC ENDPOINTS (Refactored to Strategies) ---
        
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
    }
}
