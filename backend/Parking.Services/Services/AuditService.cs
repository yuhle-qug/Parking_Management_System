using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string username, string? referenceId = null, string? details = null, bool success = true);
    }

    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _repo;

        public AuditService(IAuditLogRepository repo)
        {
            _repo = repo;
        }

        public async Task LogAsync(string action, string username, string? referenceId = null, string? details = null, bool success = true)
        {
            var log = new AuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                Action = action,
                Username = username,
                ReferenceId = referenceId,
                Details = details,
                Success = success
            };
            await _repo.AddAsync(log);
        }
    }
}
