using System;
using System.Collections.Generic;
using System.Linq;
using Parking.Core.Interfaces;

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
        public List<PeakRange> PeakRanges { get; set; } = new();

        // Convenience method that can use a default strategy or be used by one.
        // We will keep this for backward compatibility but internal implementation might change.
        public double CalculateFee(ParkingSession session) 
        {
             // For now, we delegate to a default strategy instance or keep local logic if we don't have DI here.
             // Since this is an Entity, DI is hard. 
             // Pure Domain Logic: The Policy *defines* the rules. The Strategy *executes* them.
             
             // Simplest approach: Keep the logic here but clean it up, OR Use the Strategy in the Service/Repository level.
             // But the user requested IPricingStrategy.
             
             // Let's assume we will use the Strategy in the Application Layer (Service). 
             // So here we might just keep data. 
             // BUT, to satisfy "IPricingStrategy ... instead of if-else", I should implement a Strategy class.
             
             // Let's revert this file to just Data + minimal helper.
             return new DefaultPricingStrategy().CalculateFee(session, this);
        }

        public double CalculateLostTicketFee(ParkingSession session)
        {
             return new DefaultPricingStrategy().CalculateLostTicketFee(session, this);
        }
    }

    public class PeakRange
    {
        public double StartHour { get; set; }
        public double EndHour { get; set; }
        public double Multiplier { get; set; } = 1.5;
    }
    
    // Default implementation of the strategy pattern
    public class DefaultPricingStrategy : IPricingStrategy
    {
        public double CalculateFee(ParkingSession session, PricePolicy policy)
        {
            if (session.ExitTime == null) return 0;

            var entry = session.EntryTime;
            var exit = session.ExitTime.Value;
            var duration = exit - entry;

            var hours = Math.Ceiling(duration.TotalHours);
            // Use Polymorphism from Vehicle
            var fee = hours * policy.RatePerHour * session.Vehicle.GetFeeFactor();

            var rateMultiplier = GetPeakMultiplier(entry, exit, policy.PeakRanges);
            fee *= rateMultiplier;

            if (exit.Date > entry.Date && policy.OvernightSurcharge > 0)
            {
                fee += policy.OvernightSurcharge;
            }

            if (policy.DailyMax > 0)
            {
                fee = Math.Min(fee, policy.DailyMax);
            }

            return fee;
        }

        public double CalculateLostTicketFee(ParkingSession session, PricePolicy policy)
        {
            var baseFee = CalculateFee(session, policy);
            return baseFee + policy.LostTicketFee;
        }

        private double GetPeakMultiplier(DateTime entry, DateTime exit, List<PeakRange> peakRanges)
        {
            if (peakRanges == null || peakRanges.Count == 0) return 1.0;

            bool InRange(DateTime time, PeakRange range)
            {
                var h = time.Hour + time.Minute / 60.0;
                return h >= range.StartHour && h < range.EndHour;
            }

            var maxMultiplier = peakRanges
                .Where(r => r.Multiplier > 0)
                .Where(r => InRange(entry, r) || InRange(exit, r))
                .Select(r => r.Multiplier)
                .DefaultIfEmpty(1.0)
                .Max();

            return maxMultiplier;
        }
    }
}
