using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task AddAsync(AuditLog log);
    }
}
