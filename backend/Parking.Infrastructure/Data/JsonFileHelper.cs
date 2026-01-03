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
				"ELECTRIC_BICYCLE" => new ElectricBicycle(plate),
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
				ElectricBicycle => "ELECTRIC_BICYCLE",
				Bicycle => "BICYCLE",
				_ => "CAR"
			};
		}
	}
}
