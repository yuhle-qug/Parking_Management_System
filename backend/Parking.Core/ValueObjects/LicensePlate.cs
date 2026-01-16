using System;
using System.Text.Json.Serialization;
using Parking.Core.JsonConverters;

namespace Parking.Core.ValueObjects
{
    [JsonConverter(typeof(LicensePlateConverter))]
    public class LicensePlate : IEquatable<LicensePlate>
    {
        public string Value { get; }

        private LicensePlate(string value)
        {
            Value = value;
        }

        public static LicensePlate Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("License plate cannot be empty.");

            var trimmedValue = value.Trim().ToUpperInvariant();

            // Simple validation: Ensure it has meaningful characters.
            // Adjust this regex validation as per strict Vietnamese license plate rules if needed.
            if (trimmedValue.Length < 3 || trimmedValue.Length > 15)
                 throw new ArgumentException("License plate length is invalid.");

            // Business Rule: Check for invalid characters (basic check)
            // if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedValue, @"^[A-Z0-9.\-]+$"))
            //    throw new ArgumentException("License plate contains invalid characters.");

            return new LicensePlate(trimmedValue);
        }

        public override string ToString() => Value;

        public override bool Equals(object obj) => obj is LicensePlate other && Equals(other);

        public bool Equals(LicensePlate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static implicit operator string(LicensePlate licensePlate) => licensePlate.Value;
        public static explicit operator LicensePlate(string value) => Create(value);
    }
}
