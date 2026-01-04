using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class IncidentRepository : BaseJsonRepository<Incident>, IIncidentRepository
    {
        public IncidentRepository(IHostEnvironment env) : base(env, "incidents.json")
        {
        }

        public async Task<IEnumerable<Incident>> FindOpenIncidentsAsync()
        {
            var all = await GetAllAsync();
            return all.Where(i => i.Status != "Resolved");
        }
    }
}
