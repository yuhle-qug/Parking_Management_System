using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class ParkingService : IParkingService
    {
        // [OOP - Composition]: Service "sở hữu" các Repository để làm việc
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IParkingZoneRepository _zoneRepo;
        private readonly ITicketRepository _ticketRepo;
        private readonly IGateDevice _gateDevice; // Giả lập thiết bị phần cứng

        // Constructor Injection
        public ParkingService(
            IParkingSessionRepository sessionRepo,
            IParkingZoneRepository zoneRepo,
            ITicketRepository ticketRepo,
            IGateDevice gateDevice)
        {
            _sessionRepo = sessionRepo;
            _zoneRepo = zoneRepo;
            _ticketRepo = ticketRepo;
            _gateDevice = gateDevice;
        }

        // --- USE CASE: CHECK IN (Xe vào) ---
        public async Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId)
        {
            // 1. Kiểm tra xem xe này có đang gửi trong bãi chưa (tránh duplicate)
            var activeSessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            // Trong thực tế sẽ dùng .Any(), ở đây check null/count
            foreach (var s in activeSessions)
            {
                // Nếu tìm thấy xe đang gửi -> Báo lỗi hoặc chặn lại
                throw new InvalidOperationException($"Xe {plateNumber} đang ở trong bãi, không thể check-in lại.");
            }

            // 2. Tạo đối tượng xe (Vehicle) dựa trên loại
            Vehicle vehicle = CreateVehicle(vehicleType, plateNumber);
            bool isElectric = vehicle is ElectricCar || vehicle is ElectricMotorbike || vehicle is ElectricBicycle;

            // 3. Tìm khu vực đỗ xe phù hợp (Zone)
            var zone = await _zoneRepo.FindSuitableZoneAsync(vehicleType, isElectric);
            if (zone == null)
            {
                throw new InvalidOperationException("Bãi xe đã hết chỗ hoặc không tìm thấy khu vực phù hợp.");
            }

            // 4. Tạo Vé (Ticket)
            var ticket = new Ticket
            {
                TicketId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Tạo mã ngắn gọn
                IssueTime = DateTime.Now,
                GateId = gateId
            };

            // 5. Tạo Phiên gửi xe (Session)
            var session = new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = DateTime.Now,
                Vehicle = vehicle,
                Ticket = ticket,
                Status = "Active",
                ParkingZoneId = zone.ZoneId,
                // Gán tạm Policy của Zone vào session để sau này tính tiền
                // Lưu ý: Trong thực tế Entity không nên chứa logic phức tạp, nhưng ở đây ta gán để tracking
            };

            // 6. Lưu dữ liệu (Transaction)
            // Cập nhật Zone (giảm chỗ trống - logic này nằm trong Zone.AddSession nhưng cần lưu lại state của Zone)
            zone.AddSession(session);

            await _ticketRepo.AddAsync(ticket);
            await _sessionRepo.AddAsync(session);
            // await _zoneRepo.UpdateAsync(zone); // Cập nhật lại số lượng xe trong Zone (nếu cần tracking persistent)

            // 7. Mở cổng
            await _gateDevice.OpenGateAsync(gateId);

            return session;
        }

        // --- USE CASE: CHECK OUT (Xe ra - Tính tiền) ---
        public async Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId)
        {
            // 1. Tìm phiên gửi xe
            // Thử tìm bằng TicketId trước
            var session = await _sessionRepo.FindByTicketIdAsync(ticketIdOrPlate);

            // Nếu không thấy thì tìm bằng biển số
            if (session == null)
            {
                var sessions = await _sessionRepo.FindActiveByPlateAsync(ticketIdOrPlate);
                // Lấy phiên gần nhất (thường chỉ có 1 phiên active)
                var sessionList = new List<ParkingSession>(sessions);
                if (sessionList.Count > 0)
                    session = sessionList[0];
            }

            if (session == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin gửi xe.");
            }

            // 2. Xử lý thời gian ra
            session.SetExitTime(DateTime.Now);

            // 3. Tính tiền
            // Lấy lại Zone để biết chính sách giá (hoặc lấy Default Policy)
            var zone = await _zoneRepo.GetByIdAsync(session.ParkingZoneId);

            PricePolicy policy;
            if (zone != null && zone.PricePolicy != null)
            {
                policy = zone.PricePolicy;
            }
            else
            {
                // Fallback: Dùng chính sách mặc định nếu Zone chưa config
                policy = new ParkingFeePolicy { PolicyId = "DEFAULT", Name = "Default Policy" };
            }

            // [OOP - Polymorphism]: Gọi hàm CalculateFee mà không cần quan tâm nó là Policy gì
            session.FeeAmount = policy.CalculateFee(session);
            session.Status = "PendingPayment"; // Chờ thanh toán

            // 4. Lưu cập nhật
            await _sessionRepo.UpdateAsync(session);

            // Lưu ý: Chưa mở cổng (OpenGate) ở đây. 
            // Cổng chỉ mở khi PaymentController xác nhận đã thanh toán thành công.

            return session;
        }

        // Helper: Factory Method tạo xe
        private Vehicle CreateVehicle(string type, string plate)
        {
            return type.ToUpper() switch
            {
                "CAR" => new Car(plate),
                "ELECTRIC_CAR" => new ElectricCar(plate),
                "MOTORBIKE" => new Motorbike(plate),
                "ELECTRIC_MOTORBIKE" => new ElectricMotorbike(plate),
                "BICYCLE" => new Bicycle(plate),
                _ => new Car(plate) // Default
            };
        }
    }
}