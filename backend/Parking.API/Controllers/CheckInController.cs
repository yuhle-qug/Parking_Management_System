using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // URL: api/CheckIn
    public class CheckInController : ControllerBase
    {
        private readonly IParkingService _parkingService;
        private readonly ITicketTemplateService _ticketTemplateService;

        public CheckInController(IParkingService parkingService, ITicketTemplateService ticketTemplateService)
        {
            _parkingService = parkingService;
            _ticketTemplateService = ticketTemplateService;
        }

        // --- Endpoint: Check-in logic chính ---
        // URL: POST api/CheckIn
        // Input: JSON { PlateNumber, VehicleType, GateId }
        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            try
            {
                var session = await _parkingService.CheckInAsync(request.PlateNumber, request.VehicleType, request.GateId, request.CardId);

                var isMonthly = session.Ticket.TicketId.StartsWith("M-");
                var shouldPrint = !isMonthly;

                var print = shouldPrint
                    ? _ticketTemplateService.RenderHtml(new TicketPrintData
                    {
                        GateId = request.GateId ?? string.Empty,
                        GateName = request.GateId ?? string.Empty,
                        PlateNumber = request.PlateNumber ?? string.Empty,
                        TicketId = session.Ticket.TicketId,
                        EntryTime = session.EntryTime,
                        VehicleType = request.VehicleType ?? string.Empty
                    })
                    : null;

                return Ok(new CheckInResponse
                {
                    Message = "Check-in thành công",
                    SessionId = session.SessionId,
                    TicketId = session.Ticket.TicketId,
                    ShouldPrintTicket = shouldPrint,
                    PrintHtml = print?.Html,
                    PrintFileName = print?.FileName,
                    PrintContentType = print?.ContentType
                });
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
        public string? CardId { get; set; }
    }

    public class CheckInResponse
    {
        public string Message { get; set; }
        public string SessionId { get; set; }
        public string TicketId { get; set; }
        public bool ShouldPrintTicket { get; set; }
        public string? PrintHtml { get; set; }
        public string? PrintFileName { get; set; }
        public string? PrintContentType { get; set; }
    }
}