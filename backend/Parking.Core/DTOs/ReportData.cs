using System.Collections.Generic;

namespace Parking.Core.DTOs
{
    public class ReportData
    {
        public Dictionary<string, object> ChartData { get; set; } = new();
        public List<Dictionary<string, object>> TableData { get; set; } = new();
        public Dictionary<string, object> Summary { get; set; } = new();
    }
}
