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
    public class CheckOutServiceTests
    {
        private readonly Mock<IParkingSessionRepository> _mockSessionRepo;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IPricingService> _mockPricingService;
        private readonly Mock<IGateDevice> _mockGateDevice;
        private readonly Mock<ILogger<CheckOutService>> _mockLogger;
        private readonly Mock<ITimeProvider> _mockTimeProvider;
        private readonly Mock<IMonthlyTicketRepository> _mockMonthlyTicketRepo;
        private readonly Mock<IIncidentService> _mockIncidentService;
        private readonly Mock<IParkingZoneRepository> _mockZoneRepo;
        private readonly Mock<IPricePolicyRepository> _mockPricePolicyRepo;
        private readonly CheckOutService _service;

        public CheckOutServiceTests()
        {
            _mockSessionRepo = new Mock<IParkingSessionRepository>();
            _mockValidationService = new Mock<IValidationService>();
            _mockPricingService = new Mock<IPricingService>();
            _mockGateDevice = new Mock<IGateDevice>();
            _mockLogger = new Mock<ILogger<CheckOutService>>();
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockMonthlyTicketRepo = new Mock<IMonthlyTicketRepository>();
            _mockIncidentService = new Mock<IIncidentService>();
            _mockZoneRepo = new Mock<IParkingZoneRepository>();
            _mockPricePolicyRepo = new Mock<IPricePolicyRepository>();

            _mockTimeProvider.Setup(t => t.Now).Returns(DateTime.Now);

            _service = new CheckOutService(
                _mockSessionRepo.Object,
                _mockValidationService.Object,
                _mockPricingService.Object,
                _mockGateDevice.Object,
                _mockLogger.Object,
                _mockTimeProvider.Object,
                _mockMonthlyTicketRepo.Object,
                _mockIncidentService.Object,
                _mockZoneRepo.Object,
                _mockPricePolicyRepo.Object
            );
        }

        [Fact]
        public async Task CheckOutAsync_StandardVehicle_ReturnsPendingPayment()
        {
            // Arrange
            string plate = "59-A1 12345";
            string ticketId = "TICKET-123";
            string gateId = "GATE-OUT-01";
            var session = new ParkingSession 
            { 
                SessionId = "S1", 
                Ticket = new Ticket { TicketId = ticketId, CardId = "CARD-123" },
                Vehicle = new Car(plate)
            };

            _mockSessionRepo.Setup(r => r.FindByTicketIdAsync(ticketId))
                .ReturnsAsync(session);
            _mockPricingService.Setup(p => p.CalculateFeeAsync(session))
                .ReturnsAsync(10000);

            // Act
            var result = await _service.CheckOutAsync(ticketId, gateId, plate, "CARD-123");

            // Assert
            Assert.Equal("PendingPayment", result.Status);
            Assert.Equal(10000, result.FeeAmount);
            _mockSessionRepo.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_Success_OpensGate()
        {
             // Arrange
             string sessionId = "S1";
             string exitGateId = "GATE-OUT-01";
             var session = new ParkingSession { SessionId = sessionId, Status = "PendingPayment" };

             _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId))
                 .ReturnsAsync(session);

             // Act
             var result = await _service.ConfirmPaymentAsync(sessionId, "TX-123", true, null, exitGateId);

             // Assert
             Assert.True(result.Success);
             Assert.Equal("Completed", session.Status);
             _mockGateDevice.Verify(g => g.OpenGateAsync(exitGateId), Times.Once);
        }
    }
}
