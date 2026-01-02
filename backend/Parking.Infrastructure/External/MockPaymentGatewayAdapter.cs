using System;
using System.Threading.Tasks;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.External
{
    // [OOP - Adapter Pattern]: Giả lập giao tiếp với hệ thống bên ngoài
    public class MockPaymentGatewayAdapter : IPaymentGateway
    {
        public async Task<bool> RequestPaymentAsync(double amount, string orderInfo)
        {
            Console.WriteLine($"[PAYMENT GATEWAY] Processing payment: {amount} VND - Info: {orderInfo}");
            await Task.Delay(1000); // Giả lập độ trễ mạng
            Console.WriteLine("[PAYMENT GATEWAY] Success!");
            return true; // Luôn thành công
        }
    }
}