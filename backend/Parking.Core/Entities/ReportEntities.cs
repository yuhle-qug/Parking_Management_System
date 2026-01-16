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
    }

    public class TrafficReport : Report
    {
        public int TotalVehiclesIn { get; set; }
        public int TotalVehiclesOut { get; set; }
        public Dictionary<string, int> VehiclesByType { get; set; } = new Dictionary<string, int>();
        public object HourlyTraffic { get; set; } // List<HourlyTrafficParam>
    }
}
