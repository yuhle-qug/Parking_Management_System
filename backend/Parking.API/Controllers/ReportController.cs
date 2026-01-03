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

        public ReportController(IParkingSessionRepository sessionRepo)
        {
            _sessionRepo = sessionRepo;
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

            var allSessions = await _sessionRepo.GetAllAsync();
            var completedSessions = allSessions
                .Where(s => s.Status == "Completed" &&
                        s.Payment != null &&
                        s.Payment.Time >= start &&
                        s.Payment.Time <= end)
                .ToList();

            var report = new RevenueReport
            {
                StartDate = start,
                EndDate = end,
                TotalTransactions = completedSessions.Count,
                TotalRevenue = completedSessions.Sum(s => s.Payment.Amount),
                RevenueByPaymentMethod = completedSessions
                    .GroupBy(s => s.Payment.Method)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Payment.Amount))
            };

            return Ok(report);
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

            var report = new TrafficReport
            {
                StartDate = start,
                EndDate = end,
                TotalVehiclesIn = vehiclesIn.Count,
                TotalVehiclesOut = vehiclesOut.Count,
                VehiclesByType = vehiclesIn
                    .GroupBy(s => s.Vehicle.GetType().Name)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(report);
        }
    }
}
