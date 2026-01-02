using System;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IGateDevice _gateDevice;

        // [OOP - DI]: Inject các thành phần phụ thuộc
        public PaymentService(
            IParkingSessionRepository sessionRepo,
            IPaymentGateway paymentGateway,
            IGateDevice gateDevice)
        {
            _sessionRepo = sessionRepo;
            _paymentGateway = paymentGateway;
            _gateDevice = gateDevice;
        }

        public async Task<bool> ProcessPaymentAsync(string sessionId, double amount, string method)
        {
            // 1. Kiểm tra session
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session == null) throw new ArgumentException("Session not found");

            if (session.Status == "Completed") return true; // Đã trả rồi

            // 2. Gọi qua cổng thanh toán (Adapter Pattern)
            // OrderInfo có thể là: "Thanh toan phi gui xe bien so..."
            bool isSuccess = await _paymentGateway.RequestPaymentAsync(amount, $"Parking Fee for {session.Vehicle.LicensePlate}");

            if (isSuccess)
            {
                // 3. Tạo biên lai thanh toán
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid().ToString(),
                    Amount = amount,
                    Method = method,
                    Time = DateTime.Now,
                    Status = "Completed"
                };

                // 4. Cập nhật Session
                session.AttachPayment(payment);
                session.Close(); // Đổi trạng thái thành Completed

                await _sessionRepo.UpdateAsync(session);

                // 5. Mở cổng cho xe ra (Quan trọng!)
                // Lưu ý: GateId lấy từ Ticket hoặc cấu hình lối ra
                // Ở đây giả định cổng ra cùng ID với cổng vào hoặc logic map riêng
                await _gateDevice.OpenGateAsync(session.Ticket.GateId);

                return true;
            }

            return false;
        }
    }
}