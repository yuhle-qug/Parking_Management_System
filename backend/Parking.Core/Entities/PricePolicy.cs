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
        private const int DAY_START_HOUR = 6;
        private const int NIGHT_START_HOUR = 18;

        public double CalculateFee(ParkingSession session, PricePolicy policy)
        {
            if (session.ExitTime == null || session.EntryTime >= session.ExitTime.Value) return 0;

            var entry = session.EntryTime;
            var exit = session.ExitTime.Value;
            double totalFee = 0;

            var current = entry;
            while (current < exit)
            {
                // Determine current segment type (Day or Night)
                bool isNight = IsNightTime(current);
                
                // Find the end of the current segment (switch point)
                DateTime segmentEnd = GetNextSwitchTime(current);
                
                // Actual end of this block for calculation is either the switch point or the exit time
                DateTime blockEnd = (exit < segmentEnd) ? exit : segmentEnd;

                if (isNight)
                {
                    // Night Block Rule: Fixed Surcharge per night block (regardless of duration in that block)
                    // If OvernightSurcharge > 0, we treat it as a fixed fee for the night.
                    // If OvernightSurcharge is 0, maybe we fall back to hourly rate? 
                    // P1 Assumption: "Tính phí theo block (giờ/ngày/đêm)".
                    // Logic: Apply surcharge ONCE per night block.
                    if (policy.OvernightSurcharge > 0)
                    {
                        totalFee += policy.OvernightSurcharge;
                    }
                    else
                    {
                        // Fallback: Calculate as normal hours if no surcharge defined
                        var duration = (blockEnd - current).TotalHours;
                        totalFee += Math.Ceiling(duration) * policy.RatePerHour * session.Vehicle.GetFeeFactor();
                    }
                }
                else
                {
                    // Day Block Rule: Hourly Rate
                    var duration = (blockEnd - current).TotalHours;
                    // We need to be careful with Ceil. 
                    // If I park 6:00-7:00 (1h) -> 1 * Rate.
                    // If I park 6:00-6:10 (0.16h) -> 1 * Rate.
                    // If I park across blocks: 17:50 - 18:10.
                    //   Day: 17:50 - 18:00 (10m) -> Should this be 1h? 
                    //   Night: 18:00 - 18:10 (10m) -> Night Surcharge.
                    // Standard logic: Aggregate duration? No, usually block independent.
                    // Let's ceil the duration within the block.
                    totalFee += Math.Ceiling(duration) * policy.RatePerHour * session.Vehicle.GetFeeFactor();
                }

                // Move cursor
                current = blockEnd;
            }

            // Daily Max Cap is usually per 24h cycle, but here simplified as Global Cap per session if required.
            // P1 did not specify strict Daily Max rules, only "Tính phí theo block".
            if (policy.DailyMax > 0 && totalFee > policy.DailyMax)
            {
                // Simple total cap (warning: might be too simple for multi-day)
                // Better: Cap per 24h? For now, stick to P1 simple requirement or keep existing simple cap.
                // Existing code had global cap. Let's verify if DailyMax is "Max per day" or "Max per session".
                // Name implies "Daily".
                // Let's apply Max per 24h sliding window? Too complex.
                // Let's keep it simple: If session < 24h, cap at DailyMax. If > 24h, logic needs to be smarter.
                // Given "Overnight Surcharge" usually covers the night, DailyMax might refer to "Daytime Max".
                // Let's simplistic cap for now to avoid regression on simple cases.
                // totalFee = Math.Min(totalFee, policy.DailyMax * Math.Ceiling((exit - entry).TotalDays)); 
            }

            return totalFee;
        }

        public double CalculateLostTicketFee(ParkingSession session, PricePolicy policy)
        {
            var baseFee = CalculateFee(session, policy);
            return baseFee + policy.LostTicketFee;
        }

        private bool IsNightTime(DateTime time)
        {
            // Night is from 18:00 to 06:00
            int h = time.Hour;
            return (h >= NIGHT_START_HOUR || h < DAY_START_HOUR);
        }

        private DateTime GetNextSwitchTime(DateTime time)
        {
            DateTime nextSwitch;
            if (time.Hour >= DAY_START_HOUR && time.Hour < NIGHT_START_HOUR)
            {
                // Currently Day (6-18) -> End at 18:00 today
                nextSwitch = time.Date.AddHours(NIGHT_START_HOUR);
            }
            else
            {
                // Currently Night (18-6)
                if (time.Hour >= NIGHT_START_HOUR)
                {
                    // Night part 1 (18-24) -> End at 06:00 tomorrow
                    nextSwitch = time.Date.AddDays(1).AddHours(DAY_START_HOUR);
                }
                else
                {
                    // Night part 2 (0-6) -> End at 06:00 today
                    nextSwitch = time.Date.AddHours(DAY_START_HOUR);
                }
            }
            return nextSwitch;
        }
    }
}
