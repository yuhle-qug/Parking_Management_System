using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Factories
{
    public class ParkingSessionFactory : IParkingSessionFactory
    {
        private readonly ITimeProvider _timeProvider;

        public ParkingSessionFactory(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public ParkingSession CreateNormalSession(Vehicle vehicle, Ticket ticket, string zoneId)
        {
            return new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = _timeProvider.Now,
                Vehicle = vehicle,
                Ticket = ticket,
                Status = "Active",
                ParkingZoneId = zoneId,
                CardId = ticket.CardId
            };
        }

        public ParkingSession CreateMonthlySession(Vehicle vehicle, string monthlyTicketId, string zoneId, string cardId)
        {
            var ticket = new Ticket
            {
                TicketId = monthlyTicketId,
                IssueTime = _timeProvider.Now,
                CardId = cardId
            };

            return new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = _timeProvider.Now,
                Vehicle = vehicle,
                Ticket = ticket,
                Status = "Active",
                ParkingZoneId = zoneId,
                CardId = cardId
            };
        }

        public ParkingSession CreateLostTicketSession(Vehicle vehicle, double feeAmount, string zoneId)
        {
            return new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = _timeProvider.Now,
                ExitTime = _timeProvider.Now,
                Vehicle = vehicle,
                FeeAmount = feeAmount,
                Status = "PendingPayment",
                ParkingZoneId = zoneId,
                Ticket = new Ticket
                {
                    TicketId = $"LOST-{Guid.NewGuid().ToString().Substring(0, 8)}",
                    IssueTime = _timeProvider.Now
                }
            };
        }
    }
}
