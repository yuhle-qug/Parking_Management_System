using System;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.External
{
    // [OOP - Adapter Pattern]: Giả lập giao tiếp với hệ thống bên ngoài
    public class MockPaymentGatewayAdapter : IPaymentGateway
    {
        public async Task<PaymentGatewayResult> RequestPaymentAsync(double amount, string orderInfo, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[PAYMENT GATEWAY] Creating QR for payment: {amount} VND - Info: {orderInfo}");
            await Task.Delay(800, cancellationToken); // Giả lập độ trễ mạng

            var mode = (Environment.GetEnvironmentVariable("MOCK_GATEWAY_MODE") ?? string.Empty).Trim().ToLowerInvariant();

            if (mode == "fail" || mode == "reject")
            {
                Console.WriteLine("[PAYMENT GATEWAY] Simulated failure: Accepted=false");
                return new PaymentGatewayResult
                {
                    Accepted = false,
                    Error = "Simulated gateway rejection (MOCK_GATEWAY_MODE=fail)",
                    ProviderMessage = "Mock gateway rejected request"
                };
            }

            if (mode == "timeout")
            {
                Console.WriteLine("[PAYMENT GATEWAY] Simulated timeout mode");
                await Task.Delay(5000, cancellationToken); // có thể bị hủy bởi timeout từ service
            }

            var transactionCode = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var qrPayload = $"mock-payment://pay?code={transactionCode}&amount={amount}";

            Console.WriteLine($"[PAYMENT GATEWAY] QR ready for transaction {transactionCode}");

            return new PaymentGatewayResult
            {
                Accepted = true,
                TransactionCode = transactionCode,
                QrContent = qrPayload,
                ProviderMessage = "Mock gateway generated QR"
            };
        }
    }
}