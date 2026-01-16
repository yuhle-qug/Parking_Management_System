using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlateRecognitionController : ControllerBase
    {
        private readonly IPlateRecognitionClient _plateRecognitionClient;
        private readonly ILogger<PlateRecognitionController> _logger;

        public PlateRecognitionController(
            IPlateRecognitionClient plateRecognitionClient,
            ILogger<PlateRecognitionController> logger)
        {
            _plateRecognitionClient = plateRecognitionClient;
            _logger = logger;
        }

        [HttpPost]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> Recognize([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, plates = Array.Empty<string>(), error = "File ảnh không hợp lệ." });
            }

            await using var stream = file.OpenReadStream();
            var result = await _plateRecognitionClient.RecognizeAsync(stream, file.FileName, file.ContentType ?? "image/jpeg", cancellationToken);

            if (result.IsTransportError)
            {
                _logger.LogWarning("Plate recognition service unavailable: {Message}", result.ErrorMessage);
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, plates = result.Plates, error = result.ErrorMessage });
            }

            return Ok(new { success = result.Success, plates = result.Plates, error = result.ErrorMessage });
        }
    }
}
