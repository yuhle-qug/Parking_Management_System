using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using System.Linq;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly IParkingService _parkingService;
        private readonly IPaymentService _paymentService;
        // [NEW] Repo để truy vấn danh sách phiên gửi xe
        private readonly IParkingSessionRepository _sessionRepo;

        public ParkingController(
            IParkingService parkingService,
            IPaymentService paymentService,
            IParkingSessionRepository sessionRepo)
        {
            _parkingService = parkingService;
            _paymentService = paymentService;
            _sessionRepo = sessionRepo;
        }

        // [NEW] Danh sách xe đang trong bãi
        [HttpGet("sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var allSessions = await _sessionRepo.GetAllAsync();
            var activeSessions = allSessions
                .Where(s => s.Status != "Completed")
                .OrderByDescending(s => s.EntryTime)
                .ToList();

            return Ok(activeSessions);
        }

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

    public class CheckInRequest
    {
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
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