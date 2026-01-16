																																																																																																																																																																																																																																																																																																																																																																																							using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Infrastructure.Data
{
	// Utility helper for physical JSON I/O on disk.
	public static class JsonFileHelper
	{
		private static readonly JsonSerializerOptions _options = CreateOptions();
		// Dictionary to hold a lock for each file path to ensure thread safety per file
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

		private static JsonSerializerOptions CreateOptions()
		{
			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNameCaseInsensitive = true
			};
			options.Converters.Add(new VehicleJsonConverter());
			options.Converters.Add(new PricePolicyJsonConverter());
			options.Converters.Add(new UserJsonConverter());
			return options;
		}

		private static SemaphoreSlim GetLockForFile(string filePath)
		{
			// Get or add a semaphore for the specific file path
			// Initial count 1 (mutex behavior)
			return _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
		}

		public static async Task<List<T>> ReadListAsync<T>(string filePath)
		{
			var fileLock = GetLockForFile(filePath);
			await fileLock.WaitAsync();
			try
			{
				if (!File.Exists(filePath))
				{
					Console.WriteLine($"[DEBUG] File not found: {filePath}");
					return new List<T>();
				}

				using var stream = File.OpenRead(filePath);
				var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, _options) ?? new List<T>();
				Console.WriteLine($"[DEBUG] Read {result.Count} items from {filePath}");
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[DEBUG] ERROR reading {filePath}: {ex.Message}");
				return new List<T>();
			}
			finally
			{
				fileLock.Release();
			}
		}

		public static async Task WriteListAsync<T>(string filePath, List<T> data)
		{
			var fileLock = GetLockForFile(filePath);
			await fileLock.WaitAsync();
			try
			{
				await WriteListInternalAsync(filePath, data);
			}
			finally
			{
				fileLock.Release();
			}
		}

		// Internal write method to be used when lock is already held
		private static async Task WriteListInternalAsync<T>(string filePath, List<T> data)
		{
			var directory = Path.GetDirectoryName(filePath);
			if (directory != null && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Create/Overwrite file
			using var stream = File.Create(filePath);
			await JsonSerializer.SerializeAsync(stream, data, _options);
		}

		/// <summary>
		/// Executes a read-modify-write cycle atomically for a specific file.
		/// </summary>
		/// <typeparam name="T">Entity Type</typeparam>
		/// <param name="filePath">Path to JSON file</param>
		/// <param name="action">Action to modify the list. Returns true if changes should be saved.</param>
		public static async Task ExecuteInTransactionAsync<T>(string filePath, Func<List<T>, bool> action)
		{
			var fileLock = GetLockForFile(filePath);
			await fileLock.WaitAsync();
			try
			{
				List<T> list;
				if (File.Exists(filePath))
				{
					try
					{
						using var stream = File.OpenRead(filePath);
						list = await JsonSerializer.DeserializeAsync<List<T>>(stream, _options) ?? new List<T>();
					}
					catch
					{
						list = new List<T>();
					}
				}
				else
				{
					list = new List<T>();
				}

				// Execute the modification logic
				bool shouldSave = action(list);

				if (shouldSave)
				{
					await WriteListInternalAsync(filePath, list);
				}
			}
			finally
			{
				fileLock.Release();
			}
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


	// Custom converter for UserAccount polymorphism
	internal class UserJsonConverter : JsonConverter<UserAccount>
	{
		public override UserAccount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null) return null;

			using var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			
			string GetString(string propName) => 
				root.TryGetProperty(propName, out var val) || root.TryGetProperty(propName.ToLower(), out val) 
				? (val.GetString() ?? "") : "";

			var role = GetString("Role").ToUpperInvariant();
			UserAccount user = role == "ADMIN" ? new AdminAccount() : new AttendantAccount();

			user.UserId = GetString("UserId");
			if (string.IsNullOrEmpty(user.UserId)) user.UserId = GetString("userId");
			
			user.Username = GetString("Username");
			if (string.IsNullOrEmpty(user.Username)) user.Username = GetString("username");

			user.PasswordHash = GetString("PasswordHash");
			if (string.IsNullOrEmpty(user.PasswordHash)) user.PasswordHash = GetString("passwordHash");

			user.Status = GetString("Status");
			if (string.IsNullOrEmpty(user.Status)) user.Status = GetString("status");

			return user;
		}

		public override void Write(Utf8JsonWriter writer, UserAccount value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("userId", value.UserId);
			writer.WriteString("username", value.Username);
			writer.WriteString("passwordHash", value.PasswordHash);
			writer.WriteString("role", value.Role);
			writer.WriteString("status", value.Status);
			writer.WriteEndObject();
		}
	}
}
