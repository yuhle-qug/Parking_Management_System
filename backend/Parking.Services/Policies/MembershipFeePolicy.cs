namespace Parking.Services.Policies
{
    public class MembershipFeePolicy
    {
        public double Calculate(int planMonths, string vehicleType)
        {
            var basePrice = vehicleType.ToUpperInvariant() switch
            {
                "CAR" => 2_000_000,
                "ELECTRIC_CAR" => 1_500_000,
                "MOTORBIKE" => 500_000,
                "ELECTRIC_MOTORBIKE" => 400_000,
                "BICYCLE" => 100_000,
                _ => 2_000_000
            };

            // Discount for longer plans
            var discount = planMonths switch
            {
                >= 12 => 0.15, // 15% off for 12 months
                >= 6 => 0.10,  // 10% off for 6 months
                >= 3 => 0.05,  // 5% off for 3 months
                _ => 0.0
            };

            return basePrice * planMonths * (1 - discount);
        }
    }
}
