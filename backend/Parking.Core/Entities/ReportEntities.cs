using System;
using System.Collections.Generic;

namespace Parking.Core.Entities
{
    public abstract class Report
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }

    public class RevenueReport : Report
    {
        public double TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public Dictionary<string, double> RevenueByPaymentMethod { get; set; } = new Dictionary<string, double>();
        public RevenueSummary Summary { get; set; } = new RevenueSummary();
    }

    public class RevenueSummary
    {
        public double Today { get; set; }
        public double ThisWeek { get; set; }
        public double ThisMonth { get; set; }
        public double ThisYear { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } // e.g. "27/01"
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }

    public class TrafficReport : Report
    {
        public int TotalVehiclesIn { get; set; }
        public int TotalVehiclesOut { get; set; }
        public Dictionary<string, int> VehiclesByType { get; set; } = new Dictionary<string, int>();
        public object HourlyTraffic { get; set; } // List<HourlyTrafficParam>
    }
}
