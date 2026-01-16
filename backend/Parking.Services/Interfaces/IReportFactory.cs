using Parking.Services.Interfaces;

namespace Parking.Services.Interfaces
{
    public interface IReportFactory
    {
        IReportStrategy GetStrategy(string reportType);
    }
}
