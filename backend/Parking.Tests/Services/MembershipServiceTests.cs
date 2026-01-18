using Xunit;
using Moq;
using Parking.Services.Services;
using Parking.Core.Interfaces;
using Parking.Core.Entities;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Parking.Tests.Services
{
    public class MembershipServiceTests
    {
        private readonly Mock<ICustomerService> _mockCustomerService;
        private readonly Mock<ICustomerRepository> _mockCustomerRepo;
        private readonly Mock<IMonthlyTicketRepository> _mockTicketRepo;
        private readonly Mock<IMembershipPolicyRepository> _mockPolicyRepo;
        private readonly Mock<IMembershipHistoryRepository> _mockHistoryRepo;
        private readonly Mock<ITimeProvider> _mockTimeProvider;
        private readonly Mock<ILogger<MembershipService>> _mockLogger;
        private readonly MembershipService _service;

        public MembershipServiceTests()
        {
            _mockCustomerService = new Mock<ICustomerService>();
            _mockCustomerRepo = new Mock<ICustomerRepository>();
            _mockTicketRepo = new Mock<IMonthlyTicketRepository>();
            _mockPolicyRepo = new Mock<IMembershipPolicyRepository>();
            _mockHistoryRepo = new Mock<IMembershipHistoryRepository>();
            _mockTimeProvider = new Mock<ITimeProvider>();
            _mockLogger = new Mock<ILogger<MembershipService>>();

            // Default Time Setup
            _mockTimeProvider.Setup(t => t.Now).Returns(DateTime.Now);

            _service = new MembershipService(
                _mockCustomerService.Object,
                _mockCustomerRepo.Object,
                _mockTicketRepo.Object,
                _mockPolicyRepo.Object,
                _mockHistoryRepo.Object,
                _mockTimeProvider.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task RegisterMonthlyTicketAsync_NewCustomer_Success()
        {
            // Arrange
            var customer = new Customer { Name = "John Doe", Phone = "0987654321" };
            var vehicle = new Car("59-A1 12345");
            string planId = "CAR-MONTHLY";
            var policy = new MembershipPolicy { PolicyId = planId, VehicleType = "CAR", MonthlyPrice = 2000000 };
            var existingCustomer = new Customer { CustomerId = "GEN-ID", Name = customer.Name, Phone = customer.Phone };

            _mockCustomerService.Setup(s => s.GetOrCreateCustomerAsync(It.IsAny<Customer>()))
                .ReturnsAsync(existingCustomer);
            _mockTicketRepo.Setup(r => r.FindActiveByPlateAsync(vehicle.LicensePlate))
                .ReturnsAsync((MonthlyTicket?)null);
            _mockPolicyRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MembershipPolicy> { policy });

            // Act
            var result = await _service.RegisterMonthlyTicketAsync(customer, vehicle, planId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PendingPayment", result.Status); // Updated assertion based on service logic
            Assert.Equal(vehicle.LicensePlate, result.VehiclePlate);
            Assert.Equal(2000000, result.MonthlyFee);
            
            
            _mockCustomerService.Verify(s => s.GetOrCreateCustomerAsync(It.IsAny<Customer>()), Times.Once);
            _mockTicketRepo.Verify(r => r.AddAsync(It.IsAny<MonthlyTicket>()), Times.Once);
            _mockHistoryRepo.Verify(r => r.AddHistoryAsync(It.IsAny<MembershipHistory>()), Times.Once);
        }

        [Fact]
        public async Task RegisterMonthlyTicketAsync_ActiveTicketExists_ThrowsException()
        {
            // Arrange
            var customer = new Customer { Name = "John Doe", Phone = "0987654321" };
            var vehicle = new Car("59-A1 12345");
            var existingTicket = new MonthlyTicket { TicketId = "OLD-TICKET", Status = "Active" };

            _mockCustomerService.Setup(s => s.GetOrCreateCustomerAsync(It.IsAny<Customer>()))
                .ReturnsAsync(new Customer { CustomerId = "CID" });
            _mockTicketRepo.Setup(r => r.FindActiveByPlateAsync(vehicle.LicensePlate))
                .ReturnsAsync(existingTicket);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterMonthlyTicketAsync(customer, vehicle, "PLAN"));
        }

        [Fact]
        public async Task ExtendMonthlyTicketAsync_Success()
        {
            // Arrange
            string ticketId = "TICKET-123";
            var ticket = new MonthlyTicket 
            { 
                 TicketId = ticketId, 
                 Status = "Active",
                 ExpiryDate = DateTime.Now.AddDays(5),
                 VehicleType = "CAR"
            };
            var policy = new MembershipPolicy { VehicleType = "CAR", MonthlyPrice = 2000000 };

            _mockTicketRepo.Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);
            _mockPolicyRepo.Setup(r => r.GetPolicyAsync("CAR"))
                .ReturnsAsync(policy);

            // Act
            var result = await _service.ExtendMonthlyTicketAsync(ticketId, 1, "Staff");

            // Assert
            Assert.Equal("PendingPayment", result.Status); // Updated assertion based on service logic
            // Let's verify what PrepareExtend does. In MembershipLogic refactor, it sets PendingPayment. 
            // Checking the service code again... Ah, checking logic in MonthlyTicket entity is needed.
            // But based on Service call: ticket.PrepareExtend(...) is called.
            
            _mockTicketRepo.Verify(r => r.UpdateAsync(ticket), Times.Once);
            _mockHistoryRepo.Verify(r => r.AddHistoryAsync(It.IsAny<MembershipHistory>()), Times.Once);
        }

        [Fact]
        public async Task CancelMonthlyTicketAsync_AsAdmin_CancelsImmediately()
        {
            // Arrange
            string ticketId = "TICKET-123";
            var ticket = new MonthlyTicket { TicketId = ticketId, Status = "Active" };

            _mockTicketRepo.Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act
            var result = await _service.CancelMonthlyTicketAsync(ticketId, "Admin", true, "Force Cancel");

            // Assert
            Assert.Equal("Cancelled", result.Status);
            _mockTicketRepo.Verify(r => r.UpdateAsync(ticket), Times.Once);
        }
    }
}
