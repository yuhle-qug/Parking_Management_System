using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;

namespace Parking.API.Controllers
{
    [Authorize(Roles = "ATTENDANT, ADMIN")]
    [ApiController]
    [Route("api/[controller]")] // URL: api/CheckOut
    public class CheckOutController : ControllerBase
    {
        private readonly IParkingService _parkingService;
        private readonly ICheckOutService _checkOutService;

        public CheckOutController(IParkingService parkingService, ICheckOutService checkOutService)
        {
            _parkingService = parkingService;
            _checkOutService = checkOutService;
        }

        [HttpPost]
        public async Task<IActionResult> RequestCheckOut([FromBody] CheckOutRequest request)
        {
            try
            {
                // [P3] Use new CheckOutService
                var session = await _checkOutService.CheckOutAsync(request.TicketIdOrPlate, request.GateId, request.PlateNumber, request.CardId);
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
                var session = await _checkOutService.ProcessLostTicketAsync(request.PlateNumber, request.VehicleType ?? string.Empty, request.GateId);
                var baseAmount = session.BaseFee ?? 0d;
                var lostPenalty = session.LostTicketFee ?? Math.Max(0d, session.FeeAmount - baseAmount);
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
        [AllowAnonymous]
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
        public string? VehicleType { get; set; }
        public string GateId { get; set; }
        public bool PrintReport { get; set; } = false;
    }
}
