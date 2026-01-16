using System;
using System.Collections.Generic;
using System.Linq;

namespace Parking.Core.Entities
{
    public class PricePolicy
    {
        public required string PolicyId { get; set; }
        public required string Name { get; set; }
        public string VehicleType { get; set; } = "CAR"; // CAR, MOTORBIKE, BICYCLE, ELECTRIC_CAR, ...
        public double RatePerHour { get; set; } = 10000;
        public double OvernightSurcharge { get; set; } = 30000;
        public double DailyMax { get; set; } = 200000;
        public double LostTicketFee { get; set; } = 200000;
        public List<PeakRange> PeakRanges { get; set; } = new(); // e.g., 17-21h *1.5

        public double CalculateFee(ParkingSession session)
        {
            if (session.ExitTime == null) return 0;

            var entry = session.EntryTime;
            var exit = session.ExitTime.Value;
            var duration = exit - entry;

            var hours = Math.Ceiling(duration.TotalHours);
            var fee = hours * RatePerHour * session.Vehicle.GetFeeFactor();

            var rateMultiplier = GetPeakMultiplier(entry, exit);
            fee *= rateMultiplier;

            if (exit.Date > entry.Date && OvernightSurcharge > 0)
            {
                fee += OvernightSurcharge;
            }

            if (DailyMax > 0)
            {
                fee = Math.Min(fee, DailyMax);
            }

            return fee;
        }

        public double CalculateLostTicketFee(ParkingSession session)
        {
            var baseFee = CalculateFee(session);
            return baseFee + LostTicketFee;
        }

        private double GetPeakMultiplier(DateTime entry, DateTime exit)
        {
            if (PeakRanges == null || PeakRanges.Count == 0) return 1.0;

            bool InRange(DateTime time, PeakRange range)
            {
                var h = time.Hour + time.Minute / 60.0;
                return h >= range.StartHour && h < range.EndHour;
            }

            var maxMultiplier = PeakRanges
                .Where(r => r.Multiplier > 0)
                .Where(r => InRange(entry, r) || InRange(exit, r))
                .Select(r => r.Multiplier)
                .DefaultIfEmpty(1.0)
                .Max();

            return maxMultiplier;
        }
    }

    public class PeakRange
    {
        public double StartHour { get; set; }
        public double EndHour { get; set; }
        public double Multiplier { get; set; } = 1.5;
    }
}