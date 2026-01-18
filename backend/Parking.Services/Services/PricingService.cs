using System;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class PricingService : IPricingService
    {
        private readonly IPricePolicyRepository _pricePolicyRepo;
        private readonly IParkingZoneRepository _zoneRepo;
        private readonly ITimeProvider _timeProvider;

        public PricingService(IPricePolicyRepository pricePolicyRepo, IParkingZoneRepository zoneRepo, ITimeProvider timeProvider)
        {
            _pricePolicyRepo = pricePolicyRepo;
            _zoneRepo = zoneRepo;
            _timeProvider = timeProvider;
        }

        public async Task<double> CalculateFeeAsync(ParkingSession session)
        {
            // Monthly Ticket -> Avoid duplicated logic if called explicitly, but usually handled by calling layer.
            // If ticket starts with M-, fee is 0.
            if (session.Ticket != null && session.Ticket.TicketId.ToUpper().StartsWith("M-"))
            {
                return 0;
            }

            // Temporarily set ExitTime if null to calculate fee as of NOW
            bool isExitTimeCalculated = false;
            if (session.ExitTime == null)
            {
                session.ExitTime = _timeProvider.Now;
                isExitTimeCalculated = true;
            }

            var policy = await ResolvePricePolicyAsync(session);
            var fee = policy.CalculateFee(session);

            if (isExitTimeCalculated) session.ExitTime = null;
            
            return fee;
        }

        private async Task<PricePolicy> ResolvePricePolicyAsync(ParkingSession session)
        {
            var defaultPolicy = new PricePolicy
            {
                PolicyId = "DEFAULT",
                Name = "Default Policy",
                VehicleType = session.Vehicle?.GetType().Name.ToUpperInvariant() ?? "CAR",
                RatePerHour = 10000,
                OvernightSurcharge = 30000,
                DailyMax = 200000,
                LostTicketFee = 200000
            };

            var zone = await _zoneRepo.GetByIdAsync(session.ParkingZoneId);
            var policyId = zone?.PricePolicyId;

            PricePolicy? policy = null;
            if (!string.IsNullOrWhiteSpace(policyId))
            {
                policy = await _pricePolicyRepo.GetPolicyAsync(policyId);
            }

            if (policy == null)
            {
                var vehicleType = session.Vehicle switch
                {
                    ElectricCar => "ELECTRIC_CAR",
                    Car => "CAR",
                    ElectricMotorbike => "ELECTRIC_MOTORBIKE",
                    Motorbike => "MOTORBIKE",
                    Bicycle => "BICYCLE",
                    _ => "CAR"
                };
                policy = await _pricePolicyRepo.GetPolicyByVehicleTypeAsync(vehicleType);
            }

            return policy ?? defaultPolicy;
        }
    }
}
