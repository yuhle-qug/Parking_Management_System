using System;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // URL: api/CheckIn
    public class CheckInController : ControllerBase
    {
        private readonly IParkingService _parkingService;

        public CheckInController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            try
            {
                var session = await _parkingService.CheckInAsync(request.PlateNumber, request.VehicleType, request.GateId);
                return Ok(new { Message = "Check-in thành công", SessionId = session.SessionId, TicketId = session.Ticket.TicketId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class CheckInRequest
    {
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
        public string GateId { get; set; }
    }
}
