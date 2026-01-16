using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
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

        // Constructor Injection
        public ParkingService(
            IParkingSessionRepository sessionRepo,
            IParkingZoneRepository zoneRepo,
            ITicketRepository ticketRepo,
            IGateDevice gateDevice,
            IMonthlyTicketRepository monthlyTicketRepo,
            IPricePolicyRepository pricePolicyRepo,
            ILogger<ParkingService> logger,
            IIncidentService incidentService)
        {
            _sessionRepo = sessionRepo;
            _zoneRepo = zoneRepo;
            _ticketRepo = ticketRepo;
            _gateDevice = gateDevice;
            _monthlyTicketRepo = monthlyTicketRepo;
            _pricePolicyRepo = pricePolicyRepo;
            _logger = logger;
            _incidentService = incidentService;
        }

        // --- USE CASE: CHECK IN (Xe vào) ---
        public async Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId, string? cardId = null)
        {
            var activeSessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            if (activeSessions.Any())
            {
                throw new InvalidOperationException($"Xe {plateNumber} đang ở trong bãi, không thể check-in lại.");
            }

            Vehicle vehicle = CreateVehicle(vehicleType, plateNumber);
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

            var sessionTicket = isMonthly
                ? new Ticket { TicketId = monthlyTicket!.TicketId, IssueTime = DateTime.Now, GateId = gateId, CardId = cardId }
                : new Ticket { TicketId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), IssueTime = DateTime.Now, GateId = gateId, CardId = cardId };

            var session = new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = DateTime.Now,
                Vehicle = vehicle,
                Ticket = sessionTicket,
                Status = "Active",
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
            _logger.LogInformation("Check-in thành công gate {GateId}: {Plate} -> Ticket {TicketId}, Zone {ZoneId}", gateId, plateNumber, sessionTicket.TicketId, zone.ZoneId);

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
            if (!string.Equals(normalizedPlate, session.Vehicle.LicensePlate?.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Biển số không khớp ticket {TicketId}: nhập {InputPlate}, lưu {StoredPlate}", session.Ticket?.TicketId, normalizedPlate, session.Vehicle.LicensePlate);
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

            bool isMonthly = session.Ticket.TicketId.StartsWith("M-");

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
                session.Status = "Completed";
                await _sessionRepo.UpdateAsync(session);
                await _gateDevice.OpenGateAsync(gateId);
                return session;
            }

            var policy = await ResolvePricePolicyAsync(session);
            session.FeeAmount = policy.CalculateFee(session);
            session.Status = "PendingPayment";

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
            session.Vehicle = CreateVehicle(vehicleType, session.Vehicle?.LicensePlate ?? plateNumber);
            session.SetExitTime(DateTime.Now);

            bool isMonthly = session.Ticket.TicketId.StartsWith("M-");
            var policy = await ResolvePricePolicyAsync(session);
            double baseFee = isMonthly ? 0 : policy.CalculateFee(session);
            double lostFee = isMonthly ? 0 : policy.LostTicketFee;
            double fee = isMonthly ? 0 : baseFee + lostFee;

            session.FeeAmount = fee;
            session.Status = "PendingPayment";

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
                VehicleType = session.Vehicle?.GetType().Name.ToUpperInvariant() ?? "CAR",
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
                    ElectricCar => "ELECTRIC_CAR",
                    Car => "CAR",
                    ElectricMotorbike => "ELECTRIC_MOTORBIKE",
                    Motorbike => "MOTORBIKE",
                    Bicycle => "BICYCLE",
                    _ => "CAR"
                };
                policy = await _pricePolicyRepo.GetPolicyByVehicleTypeAsync(vehicleType);
            }

            return policy ?? defaultPolicy;
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
                _ => new Car(plate) // Default fallback
            };
        }
    }
}