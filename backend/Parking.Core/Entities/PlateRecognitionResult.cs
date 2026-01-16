using System;
using System.Collections.Generic;
using System.Linq;

namespace Parking.Core.Entities
{
    public class PlateRecognitionResult
    {
        public bool Success { get; init; }

        public IReadOnlyList<string> Plates { get; init; } = Array.Empty<string>();

        public string ErrorMessage { get; init; } = string.Empty;

        public bool IsTransportError { get; init; }

        public static PlateRecognitionResult Ok(IEnumerable<string> plates)
        {
            var normalized = plates?
                .Select(p => p?.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            return new PlateRecognitionResult
            {
                Success = true,
                Plates = normalized,
                ErrorMessage = string.Empty,
                IsTransportError = false
            };
        }

        public static PlateRecognitionResult Fail(string errorMessage, bool isTransportError = false, IEnumerable<string>? plates = null)
        {
            return new PlateRecognitionResult
            {
                Success = false,
                Plates = plates?.ToArray() ?? Array.Empty<string>(),
                ErrorMessage = errorMessage ?? string.Empty,
                IsTransportError = isTransportError
            };
        }
    }
}
