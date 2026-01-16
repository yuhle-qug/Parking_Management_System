using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
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
                var session = await _parkingService.CheckOutAsync(request.TicketIdOrPlate, request.GateId, request.PlateNumber, request.CardId);
                var lostPenalty = 0d;
                var baseAmount = session.FeeAmount;
                return Ok(new
                {
                    Message = "Vui lòng thanh toán",
                    Amount = session.FeeAmount,
                    BaseAmount = baseAmount,
                    LostPenalty = lostPenalty,
                    IsLostTicket = false,
                    SessionId = session.SessionId,
                    LicensePlate = session.Vehicle.LicensePlate?.Value
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpPost("lost-ticket")]
        public async Task<IActionResult> LostTicket([FromBody] LostTicketRequest request)
        {
            try
            {
                var session = await _parkingService.ProcessLostTicketAsync(request.PlateNumber, request.VehicleType, request.GateId);
                var lostPenalty = session.FeeAmount;
                var baseAmount = 0d;
                var reportPath = request.PrintReport
                    ? @"D:\ParkingManagementSystem\backend\Mau_bien_ban_mat_ve.pdf"
                    : null;
                var reportUrl = request.PrintReport
                    ? Url.ActionLink(nameof(DownloadLostTicketReport), "CheckOut")
                    : null;
                return Ok(new
                {
                    Message = "Vui lòng thanh toán (mất vé)",
                    Amount = session.FeeAmount,
                    BaseAmount = baseAmount,
                    LostPenalty = lostPenalty,
                    IsLostTicket = true,
                    SessionId = session.SessionId,
                    LicensePlate = session.Vehicle.LicensePlate?.Value,
                    ReportFilePath = reportPath,
                    ReportUrl = reportUrl
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        // GET api/CheckOut/lost-ticket/report
        [HttpGet("lost-ticket/report")]
        public IActionResult DownloadLostTicketReport()
        {
            var path = @"D:\ParkingManagementSystem\backend\Mau_bien_ban_mat_ve.pdf";
            if (!System.IO.File.Exists(path))
            {
                return NotFound(new { Error = "Không tìm thấy file mẫu biên bản." });
            }

            var bytes = System.IO.File.ReadAllBytes(path);
            return File(bytes, "application/pdf", "Mau_bien_ban_mat_ve.pdf");
        }
    }

    public class CheckOutRequest
    {
        public string TicketIdOrPlate { get; set; }
        public string GateId { get; set; }
        public string? PlateNumber { get; set; }
        public string? CardId { get; set; }
    }

    public class LostTicketRequest
    {
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
        public string GateId { get; set; }
        public bool PrintReport { get; set; } = false;
    }
}
