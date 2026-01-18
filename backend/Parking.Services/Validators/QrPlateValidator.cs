using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Parking.Core.Interfaces;

namespace Parking.Services.Validators
{
    public class QrPlateValidator : IQrPlateValidator
    {
        public string ParsePlate(string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
                return string.Empty;

            try
            {
                // Try to parse as JSON first
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(qrData);
                return data?.GetValueOrDefault("plate") ?? qrData;
            }
            catch
            {
                // Fallback: assume QR data is the plate itself
                return qrData;
            }
        }

        public bool EnsureMatch(string plateFromQr, string plateSelected)
        {
            var normalized1 = Normalize(plateFromQr);
            var normalized2 = Normalize(plateSelected);
            return normalized1 == normalized2;
        }

        private string Normalize(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
            // Remove all non-alphanumeric characters and convert to uppercase
            return new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }
    }
}
