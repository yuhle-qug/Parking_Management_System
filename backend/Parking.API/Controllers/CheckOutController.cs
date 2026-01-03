using System;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // URL: api/CheckOut
    public class CheckOutController : ControllerBase
    {
        private readonly IParkingService _parkingService;

        public CheckOutController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        [HttpPost]
        public async Task<IActionResult> RequestCheckOut([FromBody] CheckOutRequest request)
        {
            try
            {
                var session = await _parkingService.CheckOutAsync(request.TicketIdOrPlate, request.GateId);
                return Ok(new
                {
                    Message = "Vui lòng thanh toán",
                    Amount = session.FeeAmount,
                    SessionId = session.SessionId,
                    LicensePlate = session.Vehicle.LicensePlate
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }
    }

    public class CheckOutRequest
    {
        public string TicketIdOrPlate { get; set; }
        public string GateId { get; set; }
    }
}
