namespace Parking.Core.Entities
{
    public class MembershipPolicy
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public double MonthlyPrice { get; set; }
    }
}
