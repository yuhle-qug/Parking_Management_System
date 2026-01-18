using Xunit;
using Moq;
using Parking.Services.Factories;
using Parking.Services.Strategies;
using Parking.Services.Interfaces;
using Parking.Core.Interfaces;
using System;
using Microsoft.Extensions.DependencyInjection;
using Parking.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parking.Tests.Services
{
    public class ReportTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly ReportFactory _factory;
        private readonly Mock<IParkingSessionRepository> _mockSessionRepo;

        public ReportTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _factory = new ReportFactory(_mockServiceProvider.Object);
            _mockSessionRepo = new Mock<IParkingSessionRepository>();
        }

        [Fact]
        public void GetStrategy_Revenue_ReturnsRevenueReportStrategy()
        {
            // Arrange
            _mockServiceProvider.Setup(x => x.GetService(typeof(RevenueReportStrategy)))
                .Returns(new RevenueReportStrategy(_mockSessionRepo.Object));

            // Act
            var strategy = _factory.GetStrategy("REVENUE");

            // Assert
            Assert.IsType<RevenueReportStrategy>(strategy);
        }

        [Fact]
        public void GetStrategy_Traffic_ReturnsTrafficReportStrategy()
        {
             // Arrange
            _mockServiceProvider.Setup(x => x.GetService(typeof(TrafficReportStrategy)))
                .Returns(new TrafficReportStrategy(_mockSessionRepo.Object));

            // Act
            var strategy = _factory.GetStrategy("TRAFFIC");

            // Assert
            Assert.IsType<TrafficReportStrategy>(strategy);
        }

        [Fact]
        public void GetStrategy_Invalid_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _factory.GetStrategy("INVALID"));
        }

        [Fact]
        public async Task RevenueReportStrategy_GenerateReportAsync_CalculatesCorrectly()
        {
            // Arrange
            var strategy = new RevenueReportStrategy(_mockSessionRepo.Object);
            var from = new DateTime(2023, 10, 27);
            var to = new DateTime(2023, 10, 27); // Code adds "whole day" logic

            var sessions = new List<ParkingSession>
            {
                new ParkingSession 
                { 
                    Status = "Completed", 
                    Payment = new Payment 
                    { 
                        Time = new DateTime(2023, 10, 27, 10, 0, 0), 
                        Amount = 50000, 
                        Method = "QR" 
                    } 
                },
                new ParkingSession 
                { 
                    Status = "Active", // Should be ignored
                    Payment = null 
                },
                 new ParkingSession 
                { 
                    Status = "Completed", 
                    Payment = new Payment 
                    { 
                        Time = new DateTime(2023, 10, 27, 20, 0, 0), 
                        Amount = 30000, 
                        Method = "CASH" 
                    } 
                }
            };
            
            _mockSessionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(sessions);

            // Act
            var result = await strategy.GenerateReportAsync(from, to);

            // Assert
            var report = Assert.IsType<RevenueReport>(result);
            Assert.Equal(2, report.TotalTransactions); // 2 completed with payment
            Assert.Equal(80000, report.TotalRevenue);
            Assert.Equal(50000, report.RevenueByPaymentMethod["QR"]);
            Assert.Equal(30000, report.RevenueByPaymentMethod["CASH"]);
        }
    }
}
