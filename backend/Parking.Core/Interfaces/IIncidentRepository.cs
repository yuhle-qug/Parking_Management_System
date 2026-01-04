using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IIncidentRepository : IRepository<Incident>
    {
        // Tìm các sự cố chưa giải quyết
        Task<IEnumerable<Incident>> FindOpenIncidentsAsync();
    }
}
