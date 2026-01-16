using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Parking.Services.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IMonthlyTicketRepository _ticketRepo;
        private readonly IMembershipPolicyRepository _policyRepo;
        private readonly IMembershipHistoryRepository _historyRepo;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(ICustomerRepository customerRepo, IMonthlyTicketRepository ticketRepo, IMembershipPolicyRepository policyRepo, IMembershipHistoryRepository historyRepo, ILogger<MembershipService> logger)
        {
            _customerRepo = customerRepo;
            _ticketRepo = ticketRepo;
            _policyRepo = policyRepo;
            _historyRepo = historyRepo;
            _logger = logger;
        }

        public async Task<MonthlyTicket> RegisterMonthlyTicketAsync(Customer customerInfo, Vehicle vehicle, string planId, int months = 1)
        {
            // [REAL-WORLD] Customer Code: KH-[OneTimeRandom] or Sequential
            if (string.IsNullOrWhiteSpace(customerInfo.CustomerId))
            {
                // Simple random numeric ID for demo: KH-839210
                var randomId = new Random().Next(100000, 999999);
                customerInfo.CustomerId = $"KH-{randomId}";
            }

            var existingCustomer = await _customerRepo.FindByPhoneAsync(customerInfo.Phone);
            if (existingCustomer == null)
            {
                await _customerRepo.AddAsync(customerInfo);
                existingCustomer = customerInfo;
            }

            var existingTicket = await _ticketRepo.FindActiveByPlateAsync(vehicle.LicensePlate);
            if (existingTicket != null)
            {
                _logger.LogWarning("RegisterMonthlyTicket bị từ chối: Plate {Plate} đã có vé {TicketId}", vehicle.LicensePlate, existingTicket.TicketId);
                throw new InvalidOperationException($"Xe {vehicle.LicensePlate} đã có vé tháng hiệu lực.");
            }

            var vehicleType = vehicle switch
            {
                ElectricCar => "ELECTRIC_CAR",
                Car => "CAR",
                ElectricMotorbike => "ELECTRIC_MOTORBIKE",
                Motorbike => "MOTORBIKE",
                Bicycle => "BICYCLE",
                _ => vehicle.GetType().Name.ToUpperInvariant()
            };

            var allPolicies = await _policyRepo.GetAllAsync();
            var policy = allPolicies.FirstOrDefault(p => string.Equals(p.PolicyId, planId, StringComparison.OrdinalIgnoreCase))
                        ?? allPolicies.FirstOrDefault(p => string.Equals(p.VehicleType, vehicleType, StringComparison.OrdinalIgnoreCase))
                        ?? new MembershipPolicy { PolicyId = "DEFAULT", Name = "Default", VehicleType = vehicleType, MonthlyPrice = 2_000_000 };

            if (months <= 0) months = 1;
            double fee = await CalculateFeeAsync(vehicleType, months, policy);

            // [REAL-WORLD] TicketId meant to be the Card UID (RFID)
            // Simulation: Generate a realistic RFID Hex (e.g., E004015091F66324) if manual card ID isn't implemented yet.
            var r = new Random();
            var buf = new byte[8];
            r.NextBytes(buf);
            var simulatedCardUid = BitConverter.ToString(buf).Replace("-", ""); // 16 chars HEX

            var newTicket = new MonthlyTicket
            {
                TicketId = simulatedCardUid, // Acts as the Physical Card UID
                CustomerId = existingCustomer.CustomerId,
                VehiclePlate = vehicle.LicensePlate,
                VehicleType = vehicleType,
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddMonths(months),
                Status = "PendingPayment",
                MonthlyFee = fee,
                PaymentStatus = "PendingExternal",
                PaymentAttempts = 0,
                TransactionCode = string.Empty,
                QrContent = null,
                ProviderLog = null
            };

            await _ticketRepo.AddAsync(newTicket);

            await _historyRepo.AddHistoryAsync(new MembershipHistory
            {
                HistoryId = Guid.NewGuid().ToString(),
                TicketId = newTicket.TicketId,
                Action = "Register",
                Months = months,
                Amount = fee,
                PerformedBy = GetActor(customerInfo),
                Time = DateTime.Now,
                Note = $"Plan {planId} - Generated Card: {simulatedCardUid}"
            });
            return newTicket;
        }

        public async Task<MonthlyTicket> ExtendMonthlyTicketAsync(string ticketId, int months, string performedBy, string? note = null)
        {
            if (months <= 0) throw new ArgumentException("months must be positive", nameof(months));

            var ticket = await _ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Không tìm thấy vé tháng");
            if (string.Equals(ticket.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Vé tháng đã bị hủy, không thể gia hạn");
            }

            var now = DateTime.Now;
            var baseDate = ticket.ExpiryDate > now ? ticket.ExpiryDate : now;
            var fee = await CalculateFeeAsync(ticket.VehicleType, months);

            ticket.ExpiryDate = baseDate.AddMonths(months);
            ticket.Status = "PendingPayment";
            ticket.PaymentStatus = "PendingExternal";
            ticket.PaymentAttempts = 0;
            ticket.TransactionCode = string.Empty;
            ticket.QrContent = null;
            ticket.ProviderLog = null;
            ticket.MonthlyFee = fee;

            await _ticketRepo.UpdateAsync(ticket);

            await _historyRepo.AddHistoryAsync(new MembershipHistory
            {
                HistoryId = Guid.NewGuid().ToString(),
                TicketId = ticket.TicketId,
                Action = "Extend",
                Months = months,
                Amount = fee,
                PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "system" : performedBy,
                Time = DateTime.Now,
                Note = note
            });

            return ticket;
        }

        public async Task<MonthlyTicket> CancelMonthlyTicketAsync(string ticketId, string performedBy, string? note = null)
        {
            var ticket = await _ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Không tìm thấy vé tháng");

            ticket.Status = "Cancelled";
            ticket.PaymentStatus = "Cancelled";
            ticket.ExpiryDate = DateTime.Now;
            ticket.ProviderLog = note ?? ticket.ProviderLog;

            await _ticketRepo.UpdateAsync(ticket);

            await _historyRepo.AddHistoryAsync(new MembershipHistory
            {
                HistoryId = Guid.NewGuid().ToString(),
                TicketId = ticket.TicketId,
                Action = "Cancel",
                Months = 0,
                Amount = 0,
                PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "system" : performedBy,
                Time = DateTime.Now,
                Note = note
            });

            return ticket;
        }

        // [NEW] Implement hàm lấy danh sách vé
        public async Task<IEnumerable<MonthlyTicketDto>> GetAllTicketsAsync()
        {
            var tickets = await _ticketRepo.GetAllAsync();
            var customers = (await _customerRepo.GetAllAsync())
                .Where(c => !string.IsNullOrWhiteSpace(c.CustomerId))
                .ToDictionary(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;

            // Trả về vé còn hiệu lực hoặc đang chờ thanh toán
            return tickets
                .Where(t => (t.Status == "Active" || t.Status == "PendingPayment") && t.ExpiryDate >= now)
                .Select(t =>
                {
                    customers.TryGetValue(t.CustomerId ?? string.Empty, out var customer);

                    return new MonthlyTicketDto
                    {
                        TicketId = t.TicketId,
                        CustomerId = t.CustomerId,
                        OwnerName = customer?.Name ?? string.Empty,
                        Phone = customer?.Phone ?? string.Empty,
                        IdentityNumber = customer?.IdentityNumber ?? string.Empty,
                        VehiclePlate = t.VehiclePlate,
                        VehicleType = t.VehicleType,
                        StartDate = t.StartDate,
                        ExpiryDate = t.ExpiryDate,
                        MonthlyFee = t.MonthlyFee,
                        Status = t.Status,
                        PaymentStatus = t.PaymentStatus,
                        TransactionCode = t.TransactionCode,
                        QrContent = t.QrContent,
                        ProviderLog = t.ProviderLog,
                        PaymentAttempts = t.PaymentAttempts
                    };
                });
        }

        // [NEW] Implement hàm lấy bảng giá
        public async Task<IEnumerable<MembershipPolicy>> GetAllPoliciesAsync()
        {
            return await _policyRepo.GetAllAsync();
        }

        public async Task<IEnumerable<MembershipHistory>> GetHistoryAsync(string ticketId)
        {
            return await _historyRepo.GetHistoryAsync(ticketId);
        }

        private async Task<double> CalculateFeeAsync(string vehicleType, int months, MembershipPolicy? policyOverride = null)
        {
            var policy = policyOverride ?? await _policyRepo.GetPolicyAsync(vehicleType) ?? new MembershipPolicy
            {
                PolicyId = "DEFAULT",
                Name = "Default",
                VehicleType = vehicleType,
                MonthlyPrice = 2_000_000
            };

            double fee = policy.MonthlyPrice * months;
            if (months >= 12) fee *= 0.85; // giảm 15%
            else if (months >= 6) fee *= 0.9; // giảm 10%
            else if (months >= 3) fee *= 0.95; // giảm 5%

            return fee;
        }

        private static string GetActor(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Name)) return customer.Name;
            if (!string.IsNullOrWhiteSpace(customer.Phone)) return customer.Phone;
            return "customer";
        }
    }
}
