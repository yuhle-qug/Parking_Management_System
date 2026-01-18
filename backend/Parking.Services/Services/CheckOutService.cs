using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Parking.Services.Services
{
    public class CheckOutService : ICheckOutService
    {
        private readonly IParkingSessionRepository _sessionRepo;
        private readonly IValidationService _validationService;
        private readonly IPricingService _pricingService;
        private readonly IGateDevice _gateDevice;
        private readonly ILogger<CheckOutService> _logger;
        private readonly ITimeProvider _timeProvider;
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;
        private readonly IIncidentService _incidentService;
        private readonly IParkingZoneRepository _zoneRepo;
        private readonly IPricePolicyRepository _pricePolicyRepo;

        public CheckOutService(
            IParkingSessionRepository sessionRepo,
            IValidationService validationService,
            IPricingService pricingService,
            IGateDevice gateDevice,
            ILogger<CheckOutService> logger,
            ITimeProvider timeProvider,
            IMonthlyTicketRepository monthlyTicketRepo,
            IIncidentService incidentService,
            IParkingZoneRepository zoneRepo,
            IPricePolicyRepository pricePolicyRepo)
        {
            _sessionRepo = sessionRepo;
            _validationService = validationService;
            _pricingService = pricingService;
            _gateDevice = gateDevice;
            _logger = logger;
            _timeProvider = timeProvider;
            _monthlyTicketRepo = monthlyTicketRepo;
            _incidentService = incidentService;
            _zoneRepo = zoneRepo;
            _pricePolicyRepo = pricePolicyRepo;
        }

        public async Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId, string? plateNumber = null, string? cardId = null)
        {
            // 1. Fetch Session
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

            // 2. Validate
            // Check if already completed
            if (session.Status == "Completed")
            {
                throw new InvalidOperationException($"Xe {session.Vehicle.LicensePlate} đã checkout lúc {session.ExitTime:HH:mm dd/MM/yyyy}. Không thể checkout lại.");
            }
            
            _validationService.ValidateCheckOut(session, plateNumber ?? string.Empty, cardId);

            // 3. Update Sync Info (CardId if missing)
            if (string.IsNullOrWhiteSpace(session.CardId) && !string.IsNullOrWhiteSpace(cardId))
            {
                session.CardId = cardId;
                if (session.Ticket != null) session.Ticket.CardId ??= cardId;
            }

            session.SetExitTime(_timeProvider.Now);

            // 4. Determine Fee & Status
            // Check if this is a monthly ticket by looking up in repository
            var monthlyTicket = await _monthlyTicketRepo.FindActiveByPlateAsync(session.Vehicle.LicensePlate.Value);
            bool isMonthly = monthlyTicket != null;
            
            if (isMonthly)
            {
                session.FeeAmount = 0;
                session.Status = "Completed";
                await _sessionRepo.UpdateAsync(session);
                await _gateDevice.OpenGateAsync(gateId);
                return session;
            }

            // Daily Ticket Calculation
            session.FeeAmount = await _pricingService.CalculateFeeAsync(session);
            session.Status = "PendingPayment";

            await _sessionRepo.UpdateAsync(session);
            
            return session;
        }

        public async Task<ParkingSession> ProcessLostTicketAsync(string plateNumber, string vehicleType, string gateId)
        {
            var sessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            var session = sessions.FirstOrDefault();
            if (session == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiên gửi phù hợp. Cần xác minh thủ công.");
            }

            // Cập nhật loại xe nếu khác và set giờ ra
            var resolvedVehicleType = string.IsNullOrWhiteSpace(vehicleType)
                ? GetVehicleTypeCode(session.Vehicle)
                : vehicleType;
            session.Vehicle = CreateVehicle(resolvedVehicleType, session.Vehicle?.LicensePlate?.Value ?? plateNumber);
            session.SetExitTime(_timeProvider.Now);

            bool isMonthly = session.Ticket.TicketId.StartsWith("M-");
            var policy = await ResolvePricePolicyAsync(session);
            double baseFee = isMonthly ? 0 : policy.CalculateFee(session);
            double lostFee = isMonthly ? 0 : policy.LostTicketFee;
            double fee = isMonthly ? 0 : baseFee + lostFee;

            session.FeeAmount = fee;
            session.BaseFee = baseFee;
            session.LostTicketFee = lostFee;
            session.Status = "PendingPayment";

            await _sessionRepo.UpdateAsync(session);
            _logger.LogWarning("Xử lý mất vé {Plate}: tổng phí {Fee}", plateNumber, fee);

            try
            {
                var storedCard = session.CardId ?? session.Ticket?.CardId;
                var cardNote = string.IsNullOrWhiteSpace(storedCard) ? string.Empty : $" Thẻ: {storedCard}.";
                await _incidentService.ReportIncidentAsync(
                    title: $"Mất vé - {plateNumber}",
                    description: $"Gate {gateId}, loại xe {resolvedVehicleType}, phí cơ bản {baseFee:0}, phụ thu mất vé {lostFee:0}, tổng {fee:0}.{cardNote} {(isMonthly ? "Vé tháng: miễn phụ thu" : string.Empty)}",
                    reportedBy: gateId,
                    referenceId: session.Ticket.TicketId ?? plateNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ghi nhận incident mất vé thất bại cho {Plate}", plateNumber);
            }

            return session;
        }

        public async Task<PaymentResult> ConfirmPaymentAsync(string sessionId, string transactionCode, bool success, string? providerLog = null, string? exitGateId = null)
        {
             var session = await _sessionRepo.GetByIdAsync(sessionId);
             if (session == null)
             {
                 return new PaymentResult { Success = false, Message = "Session not found" };
             }

             if (success)
             {
                 session.Status = "Completed";
                 session.ExitTime = _timeProvider.Now;
                 // session.PaymentTransactionId = transactionCode; // If Entity has this field
                 await _sessionRepo.UpdateAsync(session);

                 if (!string.IsNullOrWhiteSpace(exitGateId))
                 {
                     await _gateDevice.OpenGateAsync(exitGateId);
                 }
                 return new PaymentResult { Success = true, Message = "Payment confirmed and gate opened" };
             }
             else
             {
                 // Log failure but keep session Active/PendingPayment
                 // session.Status = "PaymentFailed"; 
                 return new PaymentResult { Success = false, Message = $"Payment failed: {providerLog}" };
             }
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
                var vehicleType = GetVehicleTypeCode(session.Vehicle);
                policy = await _pricePolicyRepo.GetPolicyByVehicleTypeAsync(vehicleType);
            }

            return policy ?? defaultPolicy;
        }

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

        private static string GetVehicleTypeCode(Vehicle? vehicle)
        {
            return vehicle switch
            {
                ElectricCar => "ELECTRIC_CAR",
                Car => "CAR",
                ElectricMotorbike => "ELECTRIC_MOTORBIKE",
                Motorbike => "MOTORBIKE",
                Bicycle => "BICYCLE",
                _ => "CAR"
            };
        }
    }
}
