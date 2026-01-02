using Microsoft.AspNetCore.Mvc;
using Parking.Core.Interfaces;
using Parking.Core.Entities;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly IParkingService _parkingService;
        private readonly IPaymentService _paymentService;

        public ParkingController(IParkingService parkingService, IPaymentService paymentService)
        {
            _parkingService = parkingService;
            _paymentService = paymentService;
        }

        // POST: api/parking/check-in
        [HttpPost("check-in")]
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

        // POST: api/parking/check-out
        // Bước 1: Yêu cầu check-out để lấy số tiền cần trả
        [HttpPost("check-out")]
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

        // POST: api/parking/pay
        // Bước 2: Thanh toán và mở cổng
        [HttpPost("pay")]
        public async Task<IActionResult> PayAndExit([FromBody] PaymentRequest request)
        {
            try
            {
                bool success = await _paymentService.ProcessPaymentAsync(request.SessionId, request.Amount, "Cash/QR");
                if (success)
                    return Ok(new { Message = "Thanh toán thành công. Cổng đang mở.", Status = "Completed" });
                else
                    return BadRequest(new { Message = "Thanh toán thất bại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    // DTOs (Data Transfer Objects) - Class chứa dữ liệu gửi lên từ Client
    public class CheckInRequest
    {
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; } // CAR, MOTORBIKE
        public string GateId { get; set; }
    }

    public class CheckOutRequest
    {
        public string TicketIdOrPlate { get; set; }
        public string GateId { get; set; }
    }

    public class PaymentRequest
    {
        public string SessionId { get; set; }
        public double Amount { get; set; }
    }
}