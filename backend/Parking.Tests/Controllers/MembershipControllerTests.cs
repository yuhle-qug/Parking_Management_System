using Xunit;
using Moq;
using Parking.API.Controllers;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Parking.Tests.Controllers
{
    public class MembershipControllerTests
    {
        private readonly Mock<IMembershipService> _mockMembershipService;
        private readonly Mock<IMonthlyTicketRepository> _mockTicketRepo;
        private readonly Mock<IMembershipPolicyRepository> _mockPolicyRepo;
        private readonly Mock<IPaymentGateway> _mockPaymentGateway;
        private readonly MembershipController _controller;

        public MembershipControllerTests()
        {
            _mockMembershipService = new Mock<IMembershipService>();
            _mockTicketRepo = new Mock<IMonthlyTicketRepository>();
            _mockPolicyRepo = new Mock<IMembershipPolicyRepository>();
            _mockPaymentGateway = new Mock<IPaymentGateway>();

            _controller = new MembershipController(
                _mockMembershipService.Object,
                _mockTicketRepo.Object,
                _mockPolicyRepo.Object,
                _mockPaymentGateway.Object
            );

             // Mock HttpContext with User
             var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
             httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
             {
                 new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser")
             }, "TestAuth"));
             _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task Register_ValidParams_ReturnsOk()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Name = "John Doe",
                Phone = "0909000000",
                IdentityNumber = "123456789",
                PlateNumber = "59-X1 12345",
                VehicleType = "CAR",
                PlanId = "PLAN-A",
                Months = 1
            };

            var ticket = new MonthlyTicket
            {
                TicketId = "T1",
                MonthlyFee = 100000,
                Status = "PendingPayment"
            };

            _mockMembershipService.Setup(s => s.RegisterMonthlyTicketAsync(
                It.IsAny<Customer>(), 
                It.IsAny<Vehicle>(), 
                request.PlanId, 
                request.Months, 
                request.VehicleBrand, 
                request.VehicleColor, 
                "testuser"))
                .ReturnsAsync(ticket);

            _mockPaymentGateway.Setup(p => p.RequestPaymentAsync(ticket.MonthlyFee, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentGatewayResult { Accepted = true, TransactionCode = "TX-111", QrContent = "QR-DATA" });

            // Act
            var result = await _controller.Register(request);

            // Assert
            // Assert
            if (result is BadRequestObjectResult badRequest)
            {
                var error = GetProperty(badRequest.Value, "Error");
                Assert.Fail($"Expected OkObjectResult but got BadRequestObjectResult. Error: {error}");
            }

            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value;
            Assert.Equal("PendingPayment", ticket.Status);
            // Verify status update in repository
            _mockTicketRepo.Verify(r => r.UpdateAsync(ticket), Times.Once); // Corrected mock object name
        }

        private object GetProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null) ?? "Unknown Error";
        }

        [Fact]
        public async Task Register_ServiceThrows_ReturnsBadRequest()
        {
             // Arrange
            var request = new RegisterRequest { PlanId = "P1", Months = 1 };
            _mockMembershipService.Setup(s => s.RegisterMonthlyTicketAsync(It.IsAny<Customer>(), It.IsAny<Vehicle>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Duplicate"));

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmPayment_Success_ActivatesTicket()
        {
            // Arrange
            var request = new ConfirmMembershipPaymentRequest
            {
                TicketId = "T1",
                Status = "SUCCESS",
                TransactionCode = "TX-NEW"
            };

            var ticket = new MonthlyTicket { TicketId = "T1", Status = "PendingPayment", ExpiryDate = DateTime.Now.AddDays(30) };

            _mockTicketRepo.Setup(r => r.GetByIdAsync("T1")).ReturnsAsync(ticket);

            // Act
            var result = await _controller.ConfirmPayment(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Active", ticket.Status);
            Assert.Equal("TX-NEW", ticket.TransactionCode);
            _mockTicketRepo.Verify(r => r.UpdateAsync(ticket), Times.Once);
        }

        [Fact]
        public async Task ConfirmPayment_Failed_UpdatesStatus()
        {
            // Arrange
            var request = new ConfirmMembershipPaymentRequest
            {
                TicketId = "T1",
                Status = "FAILED",
                ProviderLog = "Bank error"
            };

            var ticket = new MonthlyTicket { TicketId = "T1", Status = "PendingPayment" };

            _mockTicketRepo.Setup(r => r.GetByIdAsync("T1")).ReturnsAsync(ticket);

            // Act
            var result = await _controller.ConfirmPayment(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("PaymentFailed", ticket.Status);
            Assert.Contains("Bank error", ticket.ProviderLog);
        }
    }
}
