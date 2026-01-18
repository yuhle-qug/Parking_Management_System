using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IParkingSessionFactory
    {
        /// <summary>
        /// Create a normal parking session for one-time tickets
        /// </summary>
        ParkingSession CreateNormalSession(Vehicle vehicle, Ticket ticket, string zoneId);

        /// <summary>
        /// Create a parking session for monthly ticket holders
        /// </summary>
        ParkingSession CreateMonthlySession(Vehicle vehicle, string monthlyTicketId, string zoneId, string cardId);

        /// <summary>
        /// Create a parking session for lost ticket scenarios
        /// </summary>
        ParkingSession CreateLostTicketSession(Vehicle vehicle, double feeAmount, string zoneId);
    }
}
