using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parking.Core.ValueObjects;

namespace Parking.Core.JsonConverters
{
    public class LicensePlateConverter : JsonConverter<LicensePlate>
    {
        public override LicensePlate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return LicensePlate.Create(value ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, LicensePlate value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
