using System;

namespace Parking.Core.Entities
{
    public abstract class PricePolicy
    {
        public required string PolicyId { get; set; }
        public required string Name { get; set; }

        // Phương thức trừu tượng tính tiền dựa trên phiên gửi xe
        public abstract double CalculateFee(ParkingSession session);
    }

    public class ParkingFeePolicy : PricePolicy
    {
        private const double BASE_RATE_PER_HOUR = 10000; // 10k/giờ

        public override double CalculateFee(ParkingSession session)
        {
            if (session.ExitTime == null) return 0;

            var duration = session.ExitTime.Value - session.EntryTime;
            double hours = Math.Ceiling(duration.TotalHours); // Làm tròn lên

            // Công thức: Giờ * Giá cơ bản * Hệ số xe
            return hours * BASE_RATE_PER_HOUR * session.Vehicle.GetFeeFactor();
        }
    }

    public class LostTicketFeePolicy : PricePolicy
    {
        private const double PENALTY_FEE = 200000; // Phạt 200k

        public override double CalculateFee(ParkingSession session)
        {
            // Phí phạt cố định + phí gửi xe thực tế (nếu tính được)
            return PENALTY_FEE;
        }
    }
}