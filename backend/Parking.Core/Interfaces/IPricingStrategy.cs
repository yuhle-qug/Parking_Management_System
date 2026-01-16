using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IPricingStrategy
    {
        double CalculateFee(ParkingSession session, PricePolicy policy);
        double CalculateLostTicketFee(ParkingSession session, PricePolicy policy);
    }
}
