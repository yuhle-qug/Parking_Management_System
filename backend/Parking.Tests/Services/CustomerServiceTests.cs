using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Services;
using Microsoft.Extensions.Logging;

namespace Parking.Tests.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<ICustomerRepository> _mockRepo;
        private readonly Mock<ILogger<CustomerService>> _mockLogger;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _mockRepo = new Mock<ICustomerRepository>();
            _mockLogger = new Mock<ILogger<CustomerService>>();
            _service = new CustomerService(_mockRepo.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetOrCreateCustomerAsync_ExistingCustomer_ReturnsExisting()
        {
            // Arrange
            var input = new Customer { Phone = "0909000111", Name = "Nguyen Van A" };
            var existing = new Customer { CustomerId = "EXISTING-001", Phone = "0909000111", Name = "Nguyen Van A Old" };
            
            _mockRepo.Setup(r => r.FindByPhoneAsync("0909000111"))
                     .ReturnsAsync(existing);

            // Act
            var result = await _service.GetOrCreateCustomerAsync(input);

            // Assert
            Assert.Equal("EXISTING-001", result.CustomerId);
            Assert.Equal("Nguyen Van A Old", result.Name); // Should return existing without update
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateCustomerAsync_NewCustomer_CreatesAndReturnsNew()
        {
            // Arrange
            var input = new Customer { Phone = "0999888777", Name = "Le Thi B" };
            
            _mockRepo.Setup(r => r.FindByPhoneAsync("0999888777"))
                     .ReturnsAsync((Customer?)null);

            // Act
            var result = await _service.GetOrCreateCustomerAsync(input);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result.CustomerId)); // ID generated
            Assert.Equal("Le Thi B", result.Name);
            Assert.Equal("0999888777", result.Phone);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
        }
    }
}
