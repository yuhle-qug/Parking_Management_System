using Xunit;
using Moq;
using Parking.Services.Services;
using Parking.Core.Interfaces;
using Parking.Core.Entities;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Parking.Tests.Services
{
    public class ParkingServiceTests
    {
        private readonly Mock<IParkingSessionRepository> _mockSessionRepo;
        private readonly Mock<IParkingZoneRepository> _mockZoneRepo;
        private readonly Mock<ITicketRepository> _mockTicketRepo;
        private readonly Mock<IGateDevice> _mockGateDevice;
        private readonly Mock<IMonthlyTicketRepository> _mockMonthlyTicketRepo;
        private readonly Mock<IPricePolicyRepository> _mockPricePolicyRepo;
        private readonly Mock<ILogger<ParkingService>> _mockLogger;
        private readonly Mock<IIncidentService> _mockIncidentService;
        private readonly Mock<ITimeProvider> _mockTimeProvider;
        private readonly Mock<IParkingSessionFactory> _mockSessionFactory;
        private readonly ParkingService _service;

        public ParkingServiceTests()
        {
            _mockSessionRepo = new Mock<IParkingSessionRepository>();
            _mockZoneRepo = new Mock<IParkingZoneRepository>();
            _mockTicketRepo = new Mock<ITicketRepository>();
            _mockGateDevice = new Mock<IGateDevice>();
            _mockMonthlyTicketRepo = new Mock<IMonthlyTicketRepository>();
            _mockPricePolicyRepo = new Mock<IPricePolicyRepository>();
            _mockLogger = new Mock<ILogger<ParkingService>>();
            _mockIncidentService = new Mock<IIncidentService>();
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockSessionFactory = new Mock<IParkingSessionFactory>();

            // Default
            _mockTimeProvider.Setup(t => t.Now).Returns(DateTime.Now);

            _service = new ParkingService(
                _mockSessionRepo.Object,
                _mockZoneRepo.Object,
                _mockTicketRepo.Object,
                _mockGateDevice.Object,
                _mockMonthlyTicketRepo.Object,
                _mockPricePolicyRepo.Object,
                _mockLogger.Object,
                _mockIncidentService.Object,
                _mockTimeProvider.Object,
                _mockSessionFactory.Object
            );
        }

        [Fact]
        public async Task CheckInAsync_StandardVehicle_Success()
        {
            // Arrange
            string plate = "59-A1 12345";
            string type = "CAR";
            string gateId = "GATE-01";
            var zone = new ParkingZone { ZoneId = "Z1", Name = "Car Zone", Capacity = 100, VehicleCategory = "CAR" };
            
            _mockSessionRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync(new List<ParkingSession>());
            _mockMonthlyTicketRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync((MonthlyTicket?)null);
            _mockZoneRepo.Setup(r => r.FindSuitableZoneAsync(type, false, gateId))
                .ReturnsAsync(zone);
            _mockSessionRepo.Setup(r => r.CountActiveByZoneAsync("Z1"))
                .ReturnsAsync(50); // Not full

            // Setup Factory to return a valid session
            _mockSessionFactory.Setup(f => f.CreateNormalSession(It.IsAny<Vehicle>(), It.IsAny<Ticket>(), It.IsAny<string>()))
                .Returns((Vehicle v, Ticket t, string z) => new ParkingSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    EntryTime = DateTime.Now,
                    Vehicle = v,
                    Ticket = t,
                    Status = "Active",
                    ParkingZoneId = z,
                    CardId = t.CardId
                });

            // Act
            var result = await _service.CheckInAsync(plate, type, gateId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Active", result.Status);
            Assert.Equal(plate, result.Vehicle.LicensePlate.Value); // Value object
            _mockTicketRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
            _mockSessionRepo.Verify(r => r.AddAsync(It.IsAny<ParkingSession>()), Times.Once);
            _mockGateDevice.Verify(g => g.OpenGateAsync(gateId), Times.Once);
        }

        [Fact]
        public async Task CheckInAsync_VehicleAlreadyIn_ThrowsInvalidOperationException()
        {
            // Arrange
            string plate = "59-A1 12345";
            var activeSession = new ParkingSession();
            _mockSessionRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync(new List<ParkingSession> { activeSession });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CheckInAsync(plate, "CAR", "GATE-01"));
        }

        [Fact]
        public async Task CheckInAsync_ZoneFull_ThrowsInvalidOperationException()
        {
            // Arrange
            string plate = "59-A1 12345";
            string type = "CAR";
            string gateId = "GATE-01";
            var zone = new ParkingZone { ZoneId = "Z1", Name = "Full", Capacity = 10, VehicleCategory = "CAR" };

            _mockSessionRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync(new List<ParkingSession>());
            _mockMonthlyTicketRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync((MonthlyTicket?)null);
            _mockZoneRepo.Setup(r => r.FindSuitableZoneAsync(type, false, gateId))
                .ReturnsAsync(zone);
            _mockSessionRepo.Setup(r => r.CountActiveByZoneAsync("Z1"))
                .ReturnsAsync(10); // Full

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CheckInAsync(plate, type, gateId));
        }

        [Fact]
        public async Task CheckInAsync_CardAlreadyActive_ThrowsInvalidOperationException()
        {
            // Arrange
            string plate = "59-A1 99999";
            string cardId = "CARD-USED";
            
            // Mock: Plate check pass
            _mockSessionRepo.Setup(r => r.FindActiveByPlateAsync(plate))
                .ReturnsAsync(new List<ParkingSession>());
            
            // Mock: Card check fail (Pass-back detected)
            var existingSession = new ParkingSession 
            { 
                SessionId = "EXISTING", 
                CardId = cardId, 
                Vehicle = new Car("OTHER-CAR") 
            };
            _mockSessionRepo.Setup(r => r.GetActiveSessionByCardIdAsync(cardId))
                .ReturnsAsync(existingSession);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CheckInAsync(plate, "CAR", "GATE-01", cardId));
            
            Assert.Contains(cardId, ex.Message);
        }


    }
}
