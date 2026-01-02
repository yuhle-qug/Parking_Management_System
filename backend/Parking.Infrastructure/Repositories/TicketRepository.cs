using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class TicketRepository : BaseJsonRepository<Ticket>, ITicketRepository
    {
        public TicketRepository() : base("tickets.json") { }
    }
}