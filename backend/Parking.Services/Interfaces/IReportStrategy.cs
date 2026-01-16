using System;
using System.Threading.Tasks;

namespace Parking.Services.Interfaces
{
    public interface IReportStrategy
    {
        Task<object> GenerateReportAsync(DateTime from, DateTime to);
    }
}
