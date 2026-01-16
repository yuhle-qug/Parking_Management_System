using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.External
{
    public sealed class DisabledPlateRecognitionClient : IPlateRecognitionClient
    {
        private readonly ILogger<DisabledPlateRecognitionClient> _logger;

        public DisabledPlateRecognitionClient(ILogger<DisabledPlateRecognitionClient> logger)
        {
            _logger = logger;
        }

        public Task<PlateRecognitionResult> RecognizeAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Plate recognition feature is disabled; skipping external call.");
            return Task.FromResult(PlateRecognitionResult.Fail("Plate recognition feature is disabled."));
        }
    }
}
