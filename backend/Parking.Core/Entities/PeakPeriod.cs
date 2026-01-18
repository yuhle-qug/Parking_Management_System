using System;
using System.Collections.Generic;

namespace Parking.Core.Entities
{
    /// <summary>
    /// Represents a peak pricing period with time range and multiplier
    /// </summary>
    public class PeakPeriod
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double Multiplier { get; set; } = 1.0; // 1.5 = +50% surcharge
        public List<DayOfWeek> ApplicableDays { get; set; } = new();

        public bool IsApplicable(DateTime time)
        {
            var timeOfDay = time.TimeOfDay;
            var dayOfWeek = time.DayOfWeek;

            // Check if day of week matches
            if (ApplicableDays.Count > 0 && !ApplicableDays.Contains(dayOfWeek))
            {
                return false;
            }

            // Check if time is within range
            if (StartTime <= EndTime)
            {
                return timeOfDay >= StartTime && timeOfDay < EndTime;
            }
            else
            {
                // Handle overnight periods (e.g., 22:00 - 02:00)
                return timeOfDay >= StartTime || timeOfDay < EndTime;
            }
        }
    }
}
