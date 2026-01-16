using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZonesController : ControllerBase
    {
        private readonly IParkingZoneRepository _zoneRepo;
        private readonly IParkingSessionRepository _sessionRepo;

        public ZonesController(IParkingZoneRepository zoneRepo, IParkingSessionRepository sessionRepo)
        {
            _zoneRepo = zoneRepo;
            _sessionRepo = sessionRepo;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus([FromQuery] string? gateId)
        {
            var zones = await _zoneRepo.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(gateId))
            {
                var g = gateId.Trim().ToUpperInvariant();
                // Include zones that specifically list this gate OR zones that have no gate restrictions (global/empty)
                zones = zones.Where(z => 
                    (z.GateIds == null || !z.GateIds.Any()) || 
                    z.GateIds.Any(gate => string.Equals(gate, g, StringComparison.OrdinalIgnoreCase))
                );
            }

            var statusTasks = zones.Select(async z =>
            {
                var active = await _sessionRepo.CountActiveByZoneAsync(z.ZoneId);
                return new
                {
                    z.ZoneId,
                    z.Name,
                    z.VehicleCategory,
                    z.Capacity,
                    Active = active,
                    Available = Math.Max(0, z.Capacity - active),
                    IsFull = active >= z.Capacity
                };
            });

            var result = await Task.WhenAll(statusTasks);
            
            return Ok(result);
        }
    }
}
