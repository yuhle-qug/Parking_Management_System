using System;
using Microsoft.Extensions.DependencyInjection;
using Parking.Services.Interfaces;
using Parking.Services.Strategies;

namespace Parking.Services.Factories
{
    public class ReportFactory : IReportFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ReportFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IReportStrategy GetStrategy(string reportType)
        {
            // Normalize input
            if (string.IsNullOrWhiteSpace(reportType)) 
                throw new ArgumentNullException(nameof(reportType));

            return reportType.ToUpper() switch
            {
                "REVENUE" => _serviceProvider.GetRequiredService<RevenueReportStrategy>(),
                "TRAFFIC" => _serviceProvider.GetRequiredService<TrafficReportStrategy>(),
                _ => throw new ArgumentException($"Invalid report type: {reportType}")
            };
        }
    }
}
