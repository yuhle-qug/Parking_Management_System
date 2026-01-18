using Xunit;
using Moq;
using Parking.API.Controllers;
using Parking.Core.Interfaces;
using Parking.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace Parking.Tests.Controllers
{
    public class ControllerTests
    {
        private readonly Mock<IParkingService> _mockParkingService;
        private readonly Mock<ITicketTemplateService> _mockTemplateService;
        private readonly CheckInController _controller;

        public ControllerTests()
        {
            _mockParkingService = new Mock<IParkingService>();
            _mockTemplateService = new Mock<ITicketTemplateService>();
            _controller = new CheckInController(_mockParkingService.Object, _mockTemplateService.Object);
        }

        [Fact]
        public async Task CheckIn_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CheckInRequest 
            { 
                PlateNumber = "59-A1 12345", 
                VehicleType = "CAR", 
                GateId = "GATE-01" 
            };
            
            var session = new ParkingSession 
            { 
                SessionId = "S1", 
                Ticket = new Ticket { TicketId = "T1" } 
            };

            // Using It.IsAny for plate because Create returns a value object, ensuring test doesn't fail on reference mismatch if logic is complex
            _mockParkingService.Setup(s => s.CheckInAsync(It.IsAny<string>(), request.VehicleType, request.GateId, null))
                .ReturnsAsync(session);
            
            _mockTemplateService.Setup(t => t.RenderHtml(It.IsAny<TicketPrintData>()))
                .Returns(new TicketPrintResult { Html = "<html>...</html>" });

            // Act
            var result = await _controller.CheckIn(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CheckInResponse>(okResult.Value);
            Assert.Equal("Check-in thành công", response.Message);
            Assert.True(response.ShouldPrintTicket);
        }

         [Fact]
        public async Task CheckIn_ServiceThrows_ReturnsBadRequest()
        {
            // Arrange
            var request = new CheckInRequest 
            { 
                PlateNumber = "59-A1 12345", 
                VehicleType = "CAR", 
                GateId = "GATE-01" 
            };
            
            _mockParkingService.Setup(s => s.CheckInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("Garage Full"));

            // Act
            var result = await _controller.CheckIn(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
