using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Core.Configuration;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.External
{
    // Viettel ALPR client. Maps common plate fields from JSON into PlateRecognitionResult.
    public class ViettelAlprClient : IPlateRecognitionClient
    {
        private readonly HttpClient _httpClient;
        private readonly PlateRecognitionOptions _options;
        private readonly ILogger<ViettelAlprClient> _logger;

        public ViettelAlprClient(HttpClient httpClient, IOptions<PlateRecognitionOptions> options, ILogger<ViettelAlprClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = _options.GetBaseUri();
            }

            if (_httpClient.Timeout == default)
            {
                var seconds = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 30;
                _httpClient.Timeout = TimeSpan.FromSeconds(seconds);
            }
        }

        public async Task<PlateRecognitionResult> RecognizeAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (imageStream == null) throw new ArgumentNullException(nameof(imageStream));
            if (!imageStream.CanRead) throw new ArgumentException("Stream must be readable", nameof(imageStream));
            if (imageStream.CanSeek) imageStream.Position = 0;

            using var form = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            form.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "upload.jpg" : fileName);

            // Viettel yêu cầu token trong form, không phải header.
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                form.Add(new StringContent(_options.ApiKey), "token");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, ResolveEndpoint()) { Content = form };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                _logger.LogError(ex, "Không thể kết nối Viettel ALPR.");
                return PlateRecognitionResult.Fail("Plate recognition service unreachable.", true);
            }

            string responseBody = string.Empty;
            try
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không đọc được nội dung phản hồi ALPR.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Viettel ALPR trả về mã lỗi {Status} với nội dung: {Body}", (int)response.StatusCode, responseBody);
                return PlateRecognitionResult.Fail($"Remote service returned {(int)response.StatusCode}.", true);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return PlateRecognitionResult.Fail("Empty response from recognition service.");
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Viettel success: response_code == 200, number = plate.
                var responseCode = root.TryGetProperty("response_code", out var rc) && rc.TryGetInt32(out var code) ? code : -1;
                if (responseCode == 200)
                {
                    var plates = new List<string>();
                    if (root.TryGetProperty("number", out var numProp) && numProp.ValueKind == JsonValueKind.String)
                    {
                        var plate = numProp.GetString();
                        if (!string.IsNullOrWhiteSpace(plate)) plates.Add(plate.Trim());
                    }

                    if (plates.Count == 0)
                    {
                        ExtractPlates(root, plates); // fallback nếu schema thay đổi
                    }

                    if (plates.Count == 0)
                    {
                        _logger.LogWarning("Viettel ALPR trả response_code 200 nhưng không có biển số. Body: {Body}", responseBody);
                        return PlateRecognitionResult.Fail("Viettel ALPR trả về 200 nhưng không nhận diện được biển số.");
                    }

                    return PlateRecognitionResult.Ok(plates);
                }

                // Failure path: use vi_message/en_message if present
                string? error = null;
                if (root.TryGetProperty("vi_message", out var viMsg) && viMsg.ValueKind == JsonValueKind.String)
                {
                    error = viMsg.GetString();
                }
                if (string.IsNullOrWhiteSpace(error) && root.TryGetProperty("en_message", out var enMsg) && enMsg.ValueKind == JsonValueKind.String)
                {
                    error = enMsg.GetString();
                }
                if (string.IsNullOrWhiteSpace(error)) error = "Recognition failed";

                _logger.LogWarning("Viettel ALPR thất bại: {Error}; Body: {Body}", error, responseBody);

                return PlateRecognitionResult.Fail(error, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể phân tích JSON từ Viettel ALPR.");
                return PlateRecognitionResult.Fail("Unparseable recognition response.");
            }
        }

        private string ResolveEndpoint()
        {
            // BaseUrl có thể đã bao gồm đường dẫn đầy đủ (viettel cung cấp full URL detect_plate)
            var endpoint = _options.RecognizeEndpoint;
            if (string.IsNullOrWhiteSpace(endpoint)) return string.Empty; // dùng BaseAddress trực tiếp
            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return endpoint;
            return endpoint.StartsWith("/") ? endpoint : $"/{endpoint}";
        }

        private static void ExtractPlates(JsonElement element, List<string> result)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String && IsPlateProperty(property.Name))
                        {
                            var val = property.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) result.Add(val.Trim());
                        }
                        ExtractPlates(property.Value, result);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        ExtractPlates(item, result);
                    }
                    break;
                default:
                    break;
            }
        }

        private static bool IsPlateProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var lower = name.ToLowerInvariant();
            return lower.Contains("plate") || lower.Contains("license") || lower.Contains("bienso") || lower.Contains("text") || lower == "number";
        }
    }
}
