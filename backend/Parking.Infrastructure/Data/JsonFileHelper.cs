																																																																																																																																																																																																																																																																																																																																																																																							   using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Infrastructure.Data
{
	// Utility helper for physical JSON I/O on disk.
	public static class JsonFileHelper
	{
		private static readonly JsonSerializerOptions _options = CreateOptions();

		private static JsonSerializerOptions CreateOptions()
		{
			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNameCaseInsensitive = true
			};
			options.Converters.Add(new VehicleJsonConverter());
			options.Converters.Add(new PricePolicyJsonConverter());
			return options;
		}

		public static async Task<List<T>> ReadListAsync<T>(string filePath)
		{
			if (!File.Exists(filePath))
			{
				return new List<T>();
			}

			try
			{
				using var stream = File.OpenRead(filePath);
				return await JsonSerializer.DeserializeAsync<List<T>>(stream, _options) ?? new List<T>();
			}
			catch
			{
				return new List<T>();
			}
		}

		public static async Task WriteListAsync<T>(string filePath, List<T> data)
		{
			var directory = Path.GetDirectoryName(filePath);
			if (directory != null && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			using var stream = File.Create(filePath);
			await JsonSerializer.SerializeAsync(stream, data, _options);
		}
	}

	// Custom converter to persist Vehicle with type hint for polymorphic reconstruction.
	internal class VehicleJsonConverter : JsonConverter<Vehicle>
	{
		public override Vehicle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			var plate = root.TryGetProperty("LicensePlate", out var lp) ? lp.GetString() ?? string.Empty : string.Empty;
			var type = root.TryGetProperty("VehicleType", out var vt) ? vt.GetString() : null;

			return type?.ToUpperInvariant() switch
			{
				"CAR" => new Car(plate),
				"ELECTRIC_CAR" => new ElectricCar(plate),
				"MOTORBIKE" => new Motorbike(plate),
				"ELECTRIC_MOTORBIKE" => new ElectricMotorbike(plate),
				"BICYCLE" => new Bicycle(plate),
				_ => new Car(plate)
			};
		}

		public override void Write(Utf8JsonWriter writer, Vehicle value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("VehicleType", GetTypeName(value));
			writer.WriteString("LicensePlate", value.LicensePlate);
			writer.WriteEndObject();
		}

		private static string GetTypeName(Vehicle v)
		{
			return v switch
			{
				ElectricCar => "ELECTRIC_CAR",
				Car => "CAR",
				ElectricMotorbike => "ELECTRIC_MOTORBIKE",
				Motorbike => "MOTORBIKE",
				Bicycle => "BICYCLE",
				_ => "CAR"
			};
		}
	}

	// Custom converter to persist PricePolicy without recursive self-serialization.
	internal class PricePolicyJsonConverter : JsonConverter<PricePolicy>
	{
		public override PricePolicy? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
			{
				return null;
			}

			using var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;

			static bool TryGetPropertyCaseInsensitive(JsonElement element, string name, out JsonElement value)
			{
				foreach (var prop in element.EnumerateObject())
				{
					if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
					{
						value = prop.Value;
						return true;
					}
				}
				value = default;
				return false;
			}

			static double GetDouble(JsonElement element, string name, double defaultValue)
			{
				if (TryGetPropertyCaseInsensitive(element, name, out var val) && val.ValueKind == JsonValueKind.Number)
				{
					return val.GetDouble();
				}
				return defaultValue;
			}

			static string GetString(JsonElement element, string name, string defaultValue)
			{
				if (TryGetPropertyCaseInsensitive(element, name, out var val) && val.ValueKind == JsonValueKind.String)
				{
					return val.GetString() ?? defaultValue;
				}
				return defaultValue;
			}

			var policy = new PricePolicy
			{
				PolicyId = GetString(root, "PolicyId", GetString(root, "policyId", "DEFAULT")),
				Name = GetString(root, "Name", GetString(root, "name", "Default Policy")),
				VehicleType = GetString(root, "VehicleType", GetString(root, "vehicleType", "CAR")),
				RatePerHour = GetDouble(root, "RatePerHour", GetDouble(root, "ratePerHour", 0)),
				OvernightSurcharge = GetDouble(root, "OvernightSurcharge", GetDouble(root, "overnightSurcharge", 0)),
				DailyMax = GetDouble(root, "DailyMax", GetDouble(root, "dailyMax", 0)),
				LostTicketFee = GetDouble(root, "LostTicketFee", GetDouble(root, "lostTicketFee", 0)),
				PeakRanges = new List<PeakRange>()
			};

			if (TryGetPropertyCaseInsensitive(root, "PeakRanges", out var peaksElem) || TryGetPropertyCaseInsensitive(root, "peakRanges", out peaksElem))
			{
				if (peaksElem.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in peaksElem.EnumerateArray())
					{
						var start = GetDouble(item, "StartHour", GetDouble(item, "startHour", 0));
						var end = GetDouble(item, "EndHour", GetDouble(item, "endHour", 0));
						var mul = GetDouble(item, "Multiplier", GetDouble(item, "multiplier", 1));
						policy.PeakRanges.Add(new PeakRange { StartHour = start, EndHour = end, Multiplier = mul });
					}
				}
			}

			if (string.IsNullOrWhiteSpace(policy.PolicyId)) policy.PolicyId = "DEFAULT";
			if (string.IsNullOrWhiteSpace(policy.Name)) policy.Name = "Default Policy";
			if (string.IsNullOrWhiteSpace(policy.VehicleType)) policy.VehicleType = "CAR";

			return policy;
		}

		public override void Write(Utf8JsonWriter writer, PricePolicy value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("policyId", value.PolicyId);
			writer.WriteString("name", value.Name);
			writer.WriteString("vehicleType", value.VehicleType);
			writer.WriteNumber("ratePerHour", value.RatePerHour);
			writer.WriteNumber("overnightSurcharge", value.OvernightSurcharge);
			writer.WriteNumber("dailyMax", value.DailyMax);
			writer.WriteNumber("lostTicketFee", value.LostTicketFee);

			writer.WritePropertyName("peakRanges");
			writer.WriteStartArray();
			foreach (var r in value.PeakRanges ?? new List<PeakRange>())
			{
				writer.WriteStartObject();
				writer.WriteNumber("startHour", r.StartHour);
				writer.WriteNumber("endHour", r.EndHour);
				writer.WriteNumber("multiplier", r.Multiplier);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();

			writer.WriteEndObject();
		}
	}
}
