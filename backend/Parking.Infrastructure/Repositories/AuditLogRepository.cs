using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class AuditLogRepository : BaseJsonRepository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "audit_logs.json") { }
    }
}
