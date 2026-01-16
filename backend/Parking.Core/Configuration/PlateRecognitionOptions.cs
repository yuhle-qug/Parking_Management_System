using System;

namespace Parking.Core.Configuration
{
    public class PlateRecognitionOptions
    {
        public const string SectionName = "PlateRecognition";

        public bool Enabled { get; set; }

        public string BaseUrl { get; set; } = "https://viettelai.vn/plate/detect_plate";

        public string RecognizeEndpoint { get; set; } = "/recognize";

        public string Provider { get; set; } = "generic"; // generic|viettel

        public string ApiKey { get; set; } = "b369ac7174b3e4392b2358251d406854";

        public string ApiKeyHeader { get; set; } = "x-api-key";

        public int TimeoutSeconds { get; set; } = 30;

        public Uri GetBaseUri()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new InvalidOperationException("PlateRecognition:BaseUrl chưa được cấu hình.");
            }

            return new Uri(BaseUrl, UriKind.Absolute);
        }
    }
}
