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
    public class LicensePlateRecognitionClient : IPlateRecognitionClient
    {
        private readonly HttpClient _httpClient;
        private readonly PlateRecognitionOptions _options;
        private readonly ILogger<LicensePlateRecognitionClient> _logger;
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

        public LicensePlateRecognitionClient(
            HttpClient httpClient,
            IOptions<PlateRecognitionOptions> options,
            ILogger<LicensePlateRecognitionClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

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
            if (imageStream == null)
            {
                throw new ArgumentNullException(nameof(imageStream));
            }

            if (!imageStream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(imageStream));
            }

            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            using var multipart = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            var mediaType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            multipart.Add(streamContent, "file", string.IsNullOrWhiteSpace(fileName) ? "upload.jpg" : fileName);

            HttpResponseMessage response;
            try
            {
                var endpoint = string.IsNullOrWhiteSpace(_options.RecognizeEndpoint) ? "/recognize" : _options.RecognizeEndpoint;
                response = await _httpClient.PostAsync(endpoint, multipart, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                _logger.LogError(ex, "Không thể kết nối service nhận diện biển số.");
                return PlateRecognitionResult.Fail("Plate recognition service unreachable.", true);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Service nhận diện trả về mã lỗi {StatusCode} cùng nội dung: {Body}", (int)response.StatusCode, errorBody);
                return PlateRecognitionResult.Fail($"Remote service returned {(int)response.StatusCode}.", true);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync<RecognitionApiResponse>(responseStream, _serializerOptions, cancellationToken).ConfigureAwait(false);

            if (payload == null)
            {
                _logger.LogWarning("Không thể phân tích phản hồi từ service nhận diện.");
                return PlateRecognitionResult.Fail("Empty response from recognition service.", true);
            }

            if (payload.Success)
            {
                return PlateRecognitionResult.Ok(payload.Plates ?? new List<string>());
            }

            var errorMessage = string.IsNullOrWhiteSpace(payload.Error) ? "Recognition failed." : payload.Error;
            return PlateRecognitionResult.Fail(errorMessage, false, payload.Plates);
        }

        private sealed class RecognitionApiResponse
        {
            public bool Success { get; set; }
            public List<string>? Plates { get; set; }
            public string? Error { get; set; }
        }
    }
}
