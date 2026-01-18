using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Services;

namespace Parking.Tests.Services
{
    public class PricingServiceTests
    {
        private readonly Mock<IPricePolicyRepository> _mockPolicyRepo;
        private readonly Mock<IParkingZoneRepository> _mockZoneRepo;
        private readonly Mock<ITimeProvider> _mockTimeProvider;
        private readonly PricingService _service;

        public PricingServiceTests()
        {
            _mockPolicyRepo = new Mock<IPricePolicyRepository>();
            _mockZoneRepo = new Mock<IParkingZoneRepository>();
            _mockTimeProvider = new Mock<ITimeProvider>();

            _service = new PricingService(_mockPolicyRepo.Object, _mockZoneRepo.Object, _mockTimeProvider.Object);
        }

        [Fact]
        public async Task CalculateFeeAsync_ExitTimeNull_UsesTimeProviderNow()
        {
            // Arrange
            var entryTime = new DateTime(2025, 1, 1, 10, 0, 0);
            var now = new DateTime(2025, 1, 1, 11, 0, 0); // 1 Hour later
            
            _mockTimeProvider.Setup(t => t.Now).Returns(now);

            var session = new ParkingSession
            {
                Vehicle = new Car("30A-11111"),
                EntryTime = entryTime,
                ExitTime = null, // Missing exit time
                ParkingZoneId = "ZONE1"
            };

            var policy = new PricePolicy
            {
                PolicyId = "P1",
                Name = "Test",
                RatePerHour = 10000
            };

            // Mock Zone to return PolicyId
            _mockZoneRepo.Setup(z => z.GetByIdAsync("ZONE1"))
                         .ReturnsAsync(new ParkingZone { ZoneId = "ZONE1", PricePolicyId = "P1", VehicleCategory = "CAR" });
            
            // Mock Policy Repo
            _mockPolicyRepo.Setup(p => p.GetPolicyAsync("P1"))
                           .ReturnsAsync(policy);

            // Act
            var fee = await _service.CalculateFeeAsync(session);

            // Assert
            // 1 Hour * 10k = 10k
            Assert.Equal(10000, fee);
            
            // Verify TimeProvider was accessed
            _mockTimeProvider.Verify(t => t.Now, Times.Once);

            // Verify Session ExitTime is reverted to null
            Assert.Null(session.ExitTime);
        }

        [Fact]
        public async Task CalculateFeeAsync_ExitTimeProvided_UsesProvidedTime()
        {
            // Arrange
            var entryTime = new DateTime(2025, 1, 1, 10, 0, 0);
            var exitTime = new DateTime(2025, 1, 1, 12, 0, 0); // 2 Hours
            
            _mockTimeProvider.Setup(t => t.Now).Returns(new DateTime(2030, 1, 1)); // Irrelevant future date

            var session = new ParkingSession
            {
                Vehicle = new Car("30A-22222"),
                EntryTime = entryTime,
                ExitTime = exitTime,
                ParkingZoneId = "ZONE1"
            };

            var policy = new PricePolicy
            {
                PolicyId = "P1",
                Name = "Test",
                RatePerHour = 10000
            };

            _mockZoneRepo.Setup(z => z.GetByIdAsync("ZONE1"))
                         .ReturnsAsync(new ParkingZone { ZoneId = "ZONE1", PricePolicyId = "P1", VehicleCategory = "CAR" });
            
            _mockPolicyRepo.Setup(p => p.GetPolicyAsync("P1"))
                           .ReturnsAsync(policy);

            // Act
            var fee = await _service.CalculateFeeAsync(session);

            // Assert
            // 2 Hours * 10k = 20k
            Assert.Equal(20000, fee);

            // Verify TimeProvider.Now was NOT used for calculation override
            // (It might be used in other internal checks if any, but logic says if ExitTime is set, do not set Now)
            _mockTimeProvider.Verify(t => t.Now, Times.Never);
        }
    }
}
