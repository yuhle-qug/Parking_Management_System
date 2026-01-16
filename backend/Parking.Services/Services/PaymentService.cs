using System;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IPaymentGateway _paymentGateway;

        // [OOP - DI]: Inject các thành phần phụ thuộc
        // [OOP - DI]: Inject các thành phần phụ thuộc
        public PaymentService(
            IParkingSessionRepository sessionRepo,
            IPaymentGateway paymentGateway)
        {
            _sessionRepo = sessionRepo;
            _paymentGateway = paymentGateway;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(string sessionId, double amount, string method, string? exitGateId = null, int maxRetry = 3, int timeoutSeconds = 5)
        {
            var normalizedMethod = (method ?? string.Empty).Trim();
            if (string.Equals(normalizedMethod, "cash", StringComparison.OrdinalIgnoreCase) || string.Equals(normalizedMethod, "cash/qr", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Chỉ hỗ trợ thanh toán online/QR, không nhận tiền mặt.");
            }

            if (string.IsNullOrWhiteSpace(normalizedMethod))
            {
                normalizedMethod = "QR";
            }

            // 1. Kiểm tra session
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session == null) throw new ArgumentException("Session not found");

            if (session.Status == "Completed")
            {
                return new PaymentResult { Success = true, Status = "Completed", TransactionCode = session.Payment?.TransactionCode ?? string.Empty, Attempts = session.Payment?.Attempts ?? 0 };
            }

            var payment = session.Payment ?? new Payment
            {
                PaymentId = Guid.NewGuid().ToString(),
                Amount = amount,
                Method = normalizedMethod,
                Time = DateTime.Now,
                Status = "PendingExternal",
                TransactionCode = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
            };

            // đảm bảo cập nhật thông tin gateway mới nhất
            payment.Method = normalizedMethod;
            payment.Amount = amount;
            session.AttachPayment(payment);

            // Phí 0đ: bỏ qua gateway, đóng phiên và mở cổng ngay
            if (amount <= 0)
            {
                payment.MarkCompleted();
                payment.Status = "Completed";
                payment.ErrorMessage = null;
                payment.ProviderLog = "Miễn phí - không cần thanh toán";
                session.Close();

                await _sessionRepo.UpdateAsync(session);

                // Note: Gate opening is now responsibility of ParkingService (or Controller logic if needed).
                // Returning success here means payment/fee processing is done.
                // For "Free" tickets handled by ParkingService.CheckOutAsync, the gate opens there.
                // If a payment request comes here with 0 amount, we mark it paid.

                return new PaymentResult
                {
                    Success = true,
                    Status = payment.Status,
                    TransactionCode = payment.TransactionCode,
                    Attempts = payment.Attempts,
                    ProviderLog = payment.ProviderLog
                };
            }

            var orderInfo = $"Parking Fee for {session.Vehicle.LicensePlate} ({session.SessionId})";
            var attempts = 0;
            string? lastError = null;
            PaymentGatewayResult? gatewayResult = null;

            while (attempts < maxRetry && (gatewayResult == null || !gatewayResult.Accepted))
            {
                attempts++;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    gatewayResult = await _paymentGateway.RequestPaymentAsync(amount, orderInfo, cts.Token);
                    if (gatewayResult.Accepted)
                    {
                        break;
                    }

                    lastError = gatewayResult.Error ?? "Gateway refused to create QR";
                }
                catch (OperationCanceledException)
                {
                    lastError = "Gateway timeout";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }

                if (attempts < maxRetry)
                {
                    await Task.Delay(300);
                }
            }

            payment.Attempts = attempts;

            if (gatewayResult != null && gatewayResult.Accepted)
            {
                payment.Status = "PendingExternal";
                payment.ErrorMessage = null;
                payment.TransactionCode = string.IsNullOrWhiteSpace(gatewayResult.TransactionCode)
                    ? payment.TransactionCode
                    : gatewayResult.TransactionCode;
                payment.ProviderLog = gatewayResult.ProviderMessage;

                await _sessionRepo.UpdateAsync(session);

                return new PaymentResult
                {
                    Success = true,
                    Status = payment.Status,
                    TransactionCode = payment.TransactionCode,
                    Attempts = attempts,
                    QrContent = string.IsNullOrWhiteSpace(gatewayResult.PaymentUrl)
                        ? gatewayResult.QrContent
                        : gatewayResult.PaymentUrl,
                    ProviderLog = gatewayResult.ProviderMessage
                };
            }

            payment.Status = "Failed";
            payment.ErrorMessage = lastError ?? gatewayResult?.Error ?? "Gateway rejected transaction";
            await _sessionRepo.UpdateAsync(session);

            return new PaymentResult
            {
                Success = false,
                Status = payment.Status,
                TransactionCode = payment.TransactionCode,
                Error = payment.ErrorMessage,
                Attempts = attempts
            };
        }

        public async Task<PaymentResult> CancelPaymentAsync(string sessionId, string reason = "User cancelled")
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId);
            if (session == null) throw new ArgumentException("Session not found");

            var payment = session.Payment ?? new Payment
            {
                PaymentId = Guid.NewGuid().ToString(),
                Amount = session.FeeAmount,
                Method = "Unknown",
                Time = DateTime.Now,
                Status = "Pending",
                TransactionCode = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
            };

            payment.Status = "Cancelled";
            payment.ErrorMessage = reason;
            session.AttachPayment(payment);
            // Giữ session ở trạng thái PendingPayment để cho phép thanh toán lại
            await _sessionRepo.UpdateAsync(session);

            return new PaymentResult
            {
                Success = false,
                Status = payment.Status,
                TransactionCode = payment.TransactionCode,
                Error = reason,
                Attempts = payment.Attempts
            };
        }
    }
}