using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Core.Constants; // Add this
using Microsoft.Extensions.Logging;

namespace Parking.Services.Services
{
    public class ParkingService : IParkingService
    {
        // [OOP - Composition]: Service "sở hữu" các Repository để làm việc
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IParkingZoneRepository _zoneRepo;
        private readonly ITicketRepository _ticketRepo;
        private readonly IGateDevice _gateDevice; // Giả lập thiết bị phần cứng
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;
        private readonly IPricePolicyRepository _pricePolicyRepo;
        private readonly ILogger<ParkingService> _logger;
        private readonly IIncidentService _incidentService;
        private readonly IVehicleFactory _vehicleFactory; // [OCP] Factory Injection

        // Constructor Injection
        public ParkingService(
            IParkingSessionRepository sessionRepo,
            IParkingZoneRepository zoneRepo,
            ITicketRepository ticketRepo,
            IGateDevice gateDevice,
            IMonthlyTicketRepository monthlyTicketRepo,
            IPricePolicyRepository pricePolicyRepo,
            ILogger<ParkingService> logger,
            IIncidentService incidentService,
            IVehicleFactory vehicleFactory)
        {
            _sessionRepo = sessionRepo;
            _zoneRepo = zoneRepo;
            _ticketRepo = ticketRepo;
            _gateDevice = gateDevice;
            _monthlyTicketRepo = monthlyTicketRepo;
            _pricePolicyRepo = pricePolicyRepo;
            _logger = logger;
            _incidentService = incidentService;
            _vehicleFactory = vehicleFactory;
        }

        // --- USE CASE: CHECK IN (Xe vào) ---
        public async Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId, string? cardId = null)
        {
            var activeSessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            if (activeSessions.Any())
            {
                throw new InvalidOperationException($"Xe {plateNumber} đang ở trong bãi, không thể check-in lại.");
            }

            // [OCP Fix] Use Factory instead of hardcoded switch
            Vehicle vehicle = _vehicleFactory.CreateVehicle(vehicleType, plateNumber);
            bool isElectric = vehicle is ElectricCar || vehicle is ElectricMotorbike;

            var monthlyTicket = await _monthlyTicketRepo.FindActiveByPlateAsync(plateNumber);
            bool isMonthly = monthlyTicket != null;

            // Với vé tháng: cardId phải trùng mã vé tháng (TicketId) hoặc để trống sẽ auto gán mã vé tháng
            if (isMonthly)
            {
                var monthlyCard = monthlyTicket!.TicketId;
                if (!string.IsNullOrWhiteSpace(cardId) && !string.Equals(cardId.Trim(), monthlyCard, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Thẻ không khớp mã vé tháng đã đăng ký.");
                }

                cardId = monthlyCard;
            }

            var zone = await _zoneRepo.FindSuitableZoneAsync(vehicleType, isElectric, gateId);
            if (zone == null)
            {
                _logger.LogWarning("Check-in bị từ chối: đầy chỗ tại gate {GateId} cho xe {Plate} ({Type})", gateId, plateNumber, vehicleType);
                throw new InvalidOperationException($"Bãi tại cổng {gateId} đã đầy hoặc không có khu phù hợp.");
            }

            var activeInZone = await _sessionRepo.CountActiveByZoneAsync(zone.ZoneId);
            if (activeInZone >= zone.Capacity)
            {
                _logger.LogWarning("Check-in bị từ chối: Zone {ZoneId} đã đủ chỗ ({Active}/{Capacity})", zone.ZoneId, activeInZone, zone.Capacity);
                throw new InvalidOperationException($"Khu {zone.Name} đã đầy, vui lòng điều hướng qua gate khác.");
            }

            // [REAL-WORLD LOGIC] ID Generation
            string ticketId;
            if (isMonthly)
            {
                // Vé tháng: ID chính là mã thẻ (TicketId của MonthlyTicket)
                ticketId = monthlyTicket!.TicketId;
            }
            else
            {
                // Vé lượt: Sinh ID theo quy tắc [GATE]-[DATE]-[SEQ]-[HASH]
                // VD: G01-240116-0001-A1B2
                var today = DateTime.Today;
                // Note: GetAllAsync() in-memory is acceptable for this scale. In DB, use Count query.
                var dailyCount = (await _ticketRepo.GetAllAsync()).Count(t => t.IssueTime.Date == today);
                var seq = dailyCount + 1;
                
                var gateCode = gateId.Replace("GATE-", "G").Replace("gate-", "G"); // Shorten GATE-01 -> G01
                var dateCode = today.ToString("yyMMdd");
                var seqCode = seq.ToString("D4");
                var securityHash = Guid.NewGuid().ToString().Substring(0, 4).ToUpper(); // Anti-counterfeit suffix

                ticketId = $"{gateCode}-{dateCode}-{seqCode}-{securityHash}";
            }

            var sessionTicket = new Ticket 
            { 
                TicketId = ticketId, 
                IssueTime = DateTime.Now, 
                GateId = gateId, 
                CardId = cardId,
                TicketType = isMonthly ? ParkingConstants.TicketType.Monthly : ParkingConstants.TicketType.Daily
            };

            var session = new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = DateTime.Now,
                Vehicle = vehicle,
                Ticket = sessionTicket,
                Status = ParkingConstants.ParkingSessionStatus.Active,
                ParkingZoneId = zone.ZoneId,
                CardId = cardId
            };

            if (!isMonthly)
            {
                await _ticketRepo.AddAsync(sessionTicket);
            }

            zone.AddSession(session);
            await _sessionRepo.AddAsync(session);
            await _gateDevice.OpenGateAsync(gateId);
            _logger.LogInformation("Check-in thành công: {Plate} -> Ticket {TicketId}, Zone {ZoneId}", plateNumber, ticketId, zone.ZoneId);

            return session;
        }

        // --- USE CASE: CHECK OUT (Xe ra - Tính tiền) ---
        public async Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId, string? plateNumber = null, string? cardId = null)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
            {
                throw new InvalidOperationException("Biển số bắt buộc khi cho xe ra.");
            }

            var session = await _sessionRepo.FindByTicketIdAsync(ticketIdOrPlate);
            if (session == null)
            {
                var sessions = await _sessionRepo.FindActiveByPlateAsync(ticketIdOrPlate);
                session = sessions.FirstOrDefault();
            }

            if (session == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin gửi xe. Kiểm tra lại vé/thẻ hoặc xử lý mất vé.");
            }

            // Đối chiếu biển số (bắt buộc)
            var normalizedPlate = plateNumber.Trim().ToUpperInvariant();
            // LicensePlate Value Object is already trimmed and uppercased by definition usually, 
            // but for safety accessing .Value.
            // Using explicit property access fixes the 'does not contain a definition for Trim' error.
            if (!string.Equals(normalizedPlate, session.Vehicle.LicensePlate?.Value, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Biển số không khớp ticket {TicketId}: nhập {InputPlate}, lưu {StoredPlate}", session.Ticket?.TicketId, normalizedPlate, session.Vehicle.LicensePlate?.Value);
                throw new InvalidOperationException("Biển số không khớp, cần xác minh hoặc xử lý mất vé.");
            }

            // Đối chiếu thẻ (cardId) nếu đã lưu
            var storedCard = session.Ticket?.CardId ?? session.CardId;
            if (!string.IsNullOrWhiteSpace(storedCard))
            {
                var normalizedCard = (cardId ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(normalizedCard) || !string.Equals(normalizedCard, storedCard.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("CardId không khớp ticket {TicketId}: nhập {InputCard}, lưu {StoredCard}", session.Ticket?.TicketId, normalizedCard, storedCard);
                    throw new InvalidOperationException("Thẻ không khớp, cần xử lý mất vé.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(cardId))
            {
                // Nếu chưa lưu cardId, cập nhật để đồng bộ dữ liệu
                session.CardId = cardId;
                if (session.Ticket != null)
                {
                    session.Ticket.CardId ??= cardId;
                }
            }

            session.SetExitTime(DateTime.Now);

            // [FIX] Detect Monthly Ticket robustly
            // 1. New Logic: based on TicketType
            // 2. Legacy Logic: based on "M-" prefix
            bool isMonthly = session.Ticket.TicketType == ParkingConstants.TicketType.Monthly || session.Ticket.TicketId.StartsWith("M-");

            // Vé tháng: bắt buộc cardId
            if (isMonthly)
            {
                var normalizedCard = (cardId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalizedCard))
                {
                    throw new InvalidOperationException("Vé tháng cần quẹt thẻ (cardId) khi ra.");
                }
            }
            else
            {
                // Vé lượt: bắt buộc đúng TicketId
                if (!string.Equals(ticketIdOrPlate, session.Ticket?.TicketId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cần nhập đúng mã vé giấy khi ra.");
                }
            }

            if (isMonthly)
            {
                session.FeeAmount = 0;
                session.Status = ParkingConstants.ParkingSessionStatus.Completed;
                await _sessionRepo.UpdateAsync(session);
                await _gateDevice.OpenGateAsync(gateId);
                return session;
            }

            var policy = await ResolvePricePolicyAsync(session);
            session.FeeAmount = policy.CalculateFee(session);
            session.Status = ParkingConstants.ParkingSessionStatus.PendingPayment;

            await _sessionRepo.UpdateAsync(session);
            return session;
        }

        // --- USE CASE: Lost Ticket ---
        public async Task<ParkingSession> ProcessLostTicketAsync(string plateNumber, string vehicleType, string gateId)
        {
            var sessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            var session = sessions.FirstOrDefault();
            if (session == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiên gửi phù hợp. Cần xác minh thủ công.");
            }

            // Cập nhật loại xe nếu khác và set giờ ra
            session.Vehicle = _vehicleFactory.CreateVehicle(vehicleType, session.Vehicle?.LicensePlate?.Value ?? plateNumber);
            session.SetExitTime(DateTime.Now);

            bool isMonthly = session.Ticket.TicketType == ParkingConstants.TicketType.Monthly || session.Ticket.TicketId.StartsWith("M-");
            var policy = await ResolvePricePolicyAsync(session);
            double baseFee = isMonthly ? 0 : policy.CalculateFee(session);
            double lostFee = isMonthly ? 0 : policy.LostTicketFee;
            double fee = isMonthly ? 0 : baseFee + lostFee;

            session.FeeAmount = fee;
            session.Status = ParkingConstants.ParkingSessionStatus.PendingPayment;

            await _sessionRepo.UpdateAsync(session);
            _logger.LogWarning("Xử lý mất vé {Plate}: tổng phí {Fee}", plateNumber, fee);

            try
            {
                var storedCard = session.CardId ?? session.Ticket?.CardId;
                var cardNote = string.IsNullOrWhiteSpace(storedCard) ? string.Empty : $" Thẻ: {storedCard}.";
                await _incidentService.ReportIncidentAsync(
                    title: $"Mất vé - {plateNumber}",
                    description: $"Gate {gateId}, loại xe {vehicleType}, phí cơ bản {baseFee:0}, phụ thu mất vé {lostFee:0}, tổng {fee:0}.{cardNote} {(isMonthly ? "Vé tháng: miễn phụ thu" : string.Empty)}",
                    reportedBy: gateId,
                    referenceId: session.Ticket.TicketId ?? plateNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ghi nhận incident mất vé thất bại cho {Plate}", plateNumber);
            }

            return session;
        }

        private async Task<PricePolicy> ResolvePricePolicyAsync(ParkingSession session)
        {
            var defaultPolicy = new PricePolicy
            {
                PolicyId = "DEFAULT",
                Name = "Default Policy",
                VehicleType = session.Vehicle?.GetType().Name.ToUpperInvariant() ?? ParkingConstants.VehicleType.Car,
                RatePerHour = 10000,
                OvernightSurcharge = 30000,
                DailyMax = 200000,
                LostTicketFee = 200000
            };

            var zone = await _zoneRepo.GetByIdAsync(session.ParkingZoneId);
            var policyId = zone?.PricePolicyId;

            PricePolicy? policy = null;
            if (!string.IsNullOrWhiteSpace(policyId))
            {
                policy = await _pricePolicyRepo.GetPolicyAsync(policyId);
            }

            if (policy == null)
            {
                var vehicleType = session.Vehicle switch
                {
                    ElectricCar => ParkingConstants.VehicleType.ElectricCar,
                    Car => ParkingConstants.VehicleType.Car,
                    ElectricMotorbike => ParkingConstants.VehicleType.ElectricMotorbike,
                    Motorbike => ParkingConstants.VehicleType.Motorbike,
                    Bicycle => ParkingConstants.VehicleType.Bicycle,
                    _ => ParkingConstants.VehicleType.Car
                };
                policy = await _pricePolicyRepo.GetPolicyByVehicleTypeAsync(vehicleType);
            }

            return policy ?? defaultPolicy;
        }
    }
}