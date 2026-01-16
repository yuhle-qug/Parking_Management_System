using System.Collections.Generic;
using System.Linq;

namespace Parking.Core.Entities
{
    public class ParkingZone
    {
        public string ZoneId { get; set; }
        public string Name { get; set; }
        public required string VehicleCategory { get; set; } // CAR, MOTORBIKE, BICYCLE
        public bool ElectricOnly { get; set; }
        public int Capacity { get; set; }
        public List<string> GateIds { get; set; } = new List<string>(); // Gate(s) phục vụ zone này
        public string? PricePolicyId { get; set; }

        // Quan hệ 1-n: Một khu vực chứa nhiều phiên gửi xe
        public List<ParkingSession> ActiveSessions { get; set; } = new List<ParkingSession>();

        public bool IsFull()
        {
            return ActiveSessions.Count >= Capacity;
        }

        public void AddSession(ParkingSession session)
        {
            if (!IsFull())
            {
                ActiveSessions.Add(session);
                session.ParkingZoneId = this.ZoneId;
            }
        }

        public void RemoveSession(ParkingSession session)
        {
            ActiveSessions.Remove(session);
        }
    }

    public class ParkingLot
    {
        public required string Name { get; set; }
        public List<ParkingZone> Zones { get; set; } = new List<ParkingZone>();

        // Logic tìm khu vực đỗ xe phù hợp
        public ParkingZone? FindZoneFor(Vehicle vehicle, string gateId)
        {
            // Logic đơn giản: Tìm khu vực khớp loại xe và còn chỗ
            // (Trong thực tế có thể phức tạp hơn dựa trên gateId)

            bool isElectric = vehicle is ElectricCar || vehicle is ElectricMotorbike;
            string category = GetVehicleCategory(vehicle);

            return Zones.FirstOrDefault(z =>
                z.VehicleCategory == category &&
                (!z.ElectricOnly || isElectric) && // Nếu khu vực chỉ cho xe điện thì xe phải là điện
                !z.IsFull()
            );
        }

        private string GetVehicleCategory(Vehicle vehicle)
        {
            if (vehicle is Car) return "CAR";
            if (vehicle is Motorbike) return "MOTORBIKE";
            if (vehicle is Bicycle) return "BICYCLE";
            return "UNKNOWN";
        }
    }
}