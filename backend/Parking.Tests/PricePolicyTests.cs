using System;
using Xunit;
using Parking.Core.Entities;

namespace Parking.Tests
{
    public class PricePolicyTests
    {
        private readonly PricePolicy _policy;

        public PricePolicyTests()
        {
            _policy = new PricePolicy
            {
                PolicyId = "TEST",
                Name = "Test Policy",
                RatePerHour = 10000,
                OvernightSurcharge = 30000,
                DailyMax = 200000
            };
        }

        [Fact]
        public void CalculateFee_Daytime_1Hour()
        {
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 8, 0, 0),
                ExitTime = new DateTime(2025, 1, 1, 9, 0, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(10000, fee); // 1h * 10k
        }

        [Fact]
        public void CalculateFee_Daytime_PartialHour()
        {
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 8, 0, 0),
                ExitTime = new DateTime(2025, 1, 1, 8, 15, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(10000, fee); // ceil(0.25) -> 1h * 10k
        }

        [Fact]
        public void CalculateFee_Overnight_Simple()
        {
            // Enter 22:00 (Night), Exit 23:00 (Night)
            // Should be 1 Night Surcharge = 30k
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 22, 0, 0),
                ExitTime = new DateTime(2025, 1, 1, 23, 0, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(30000, fee);
        }

        [Fact]
        public void CalculateFee_DayAndNight()
        {
            // Enter 17:00 (Day), Exit 19:00 (Night)
            // Day: 17:00-18:00 -> 1h -> 10k
            // Night: 18:00-19:00 -> Night Block -> 30k
            // Total: 40k
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 17, 0, 0),
                ExitTime = new DateTime(2025, 1, 1, 19, 0, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(40000, fee);
        }

        [Fact]
        public void CalculateFee_Overnight_NewDay()
        {
            // Enter 23:00 (Night), Exit 07:00 next day
            // Night: 23:00-06:00 -> 30k
            // Day: 06:00-07:00 -> 1h -> 10k
            // Total: 40k
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 23, 0, 0),
                ExitTime = new DateTime(2025, 1, 2, 7, 0, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(40000, fee);
        }

        [Fact]
        public void CalculateFee_MultiDay()
        {
            // Day 1 (17:00-18:00) -> 10k
            // Night 1 (18:00-06:00) -> 30k
            // Day 2 (06:00-18:00) -> 12h * 10k = 120k
            // Night 2 (18:00-06:00) -> 30k
            // Day 3 (06:00-07:00) -> 10k
            // Total: 10 + 30 + 120 + 30 + 10 = 200k
            var session = new ParkingSession
            {
                Vehicle = new Car("30A-12345"),
                EntryTime = new DateTime(2025, 1, 1, 17, 0, 0),
                ExitTime = new DateTime(2025, 1, 3, 7, 0, 0)
            };

            var fee = _policy.CalculateFee(session);
            Assert.Equal(200000, fee);
        }
    }
}
