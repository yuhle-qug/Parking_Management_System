using Microsoft.AspNetCore.Mvc;
using Parking.Services.Services;
using System;
using System.Threading.Tasks;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        [HttpPost("report")]
        public async Task<IActionResult> Report([FromBody] CreateIncidentRequest request)
        {
            try
            {
                var incident = await _incidentService.ReportIncidentAsync(
                    request.Title,
                    request.Description,
                    request.ReportedBy,
                    request.ReferenceId
                );
                return Ok(incident);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve([FromBody] ResolveIncidentRequest request)
        {
            var success = await _incidentService.ResolveIncidentAsync(request.IncidentId, request.ResolutionNotes);
            if (success) return Ok(new { Message = "Sự cố đã được giải quyết" });
            return NotFound(new { Error = "Không tìm thấy sự cố" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _incidentService.GetAllIncidentsAsync();
            return Ok(list);
        }
    }

    public class CreateIncidentRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReportedBy { get; set; }
        public string ReferenceId { get; set; }
    }

    public class ResolveIncidentRequest
    {
        public string IncidentId { get; set; }
        public string ResolutionNotes { get; set; }
    }
}
