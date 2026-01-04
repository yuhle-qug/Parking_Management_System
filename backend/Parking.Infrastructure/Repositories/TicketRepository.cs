using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class TicketRepository : BaseJsonRepository<Ticket>, ITicketRepository
    {
        public TicketRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "tickets.json") { }
    }
}