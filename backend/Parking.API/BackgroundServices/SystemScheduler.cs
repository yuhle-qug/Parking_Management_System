using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Interfaces;

namespace Parking.API.BackgroundServices
{
    // Class này kế thừa BackgroundService của .NET để chạy ngầm
    public class SystemScheduler : BackgroundService
    {
        // Vì BackgroundService là Singleton (sống suốt đời app)
        // còn Repository là Scoped (sống theo request), nên ta cần ServiceProvider để tạo scope thủ công.
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SystemScheduler> _logger;

        public SystemScheduler(IServiceProvider serviceProvider, ILogger<SystemScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // Hàm này sẽ chạy ngay khi server khởi động
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("⏳ System Scheduler đang khởi động...");

            // Vòng lặp chạy mãi mãi cho đến khi tắt server
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiredMonthlyTickets();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi trong quá trình chạy Scheduler");
                }

                // Nghỉ 60 giây trước khi quét lần tiếp theo (tránh tốn tài nguyên)
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }

            _logger.LogInformation("🛑 System Scheduler đã dừng.");
        }

        private async Task CheckExpiredMonthlyTickets()
        {
            // Tạo một Scope mới (giống như giả lập 1 request) để lấy Repository
            using (var scope = _serviceProvider.CreateScope())
            {
                var ticketRepo = scope.ServiceProvider.GetRequiredService<IMonthlyTicketRepository>();

                // 1. Lấy tất cả vé tháng
                var allTickets = await ticketRepo.GetAllAsync();

                // 2. Lọc ra các vé đang Active nhưng ngày hết hạn đã qua (Quá khứ)
                var expiredTickets = allTickets
                    .Where(t => t.Status == "Active" && t.ExpiryDate < DateTime.Now)
                    .ToList();

                // 3. Cập nhật trạng thái
                if (expiredTickets.Any())
                {
                    _logger.LogInformation($"[Scheduler] Tìm thấy {expiredTickets.Count} vé hết hạn. Đang xử lý...");

                    foreach (var ticket in expiredTickets)
                    {
                        ticket.Status = "Expired";
                        await ticketRepo.UpdateAsync(ticket);
                        _logger.LogInformation($"   -> Đã khóa vé: {ticket.TicketId} (Biển số: {ticket.VehiclePlate})");
                    }
                }
            }
        }
    }
}
