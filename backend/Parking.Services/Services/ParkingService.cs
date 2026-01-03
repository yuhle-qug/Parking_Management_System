using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;

        // Constructor Injection
        public ParkingService(
            IParkingSessionRepository sessionRepo,
            IParkingZoneRepository zoneRepo,
            ITicketRepository ticketRepo,
            IGateDevice gateDevice,
            IMonthlyTicketRepository monthlyTicketRepo)
        {
            _sessionRepo = sessionRepo;
            _zoneRepo = zoneRepo;
            _ticketRepo = ticketRepo;
            _gateDevice = gateDevice;
            _monthlyTicketRepo = monthlyTicketRepo;
        }

        // --- USE CASE: CHECK IN (Xe vào) ---
        public async Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId)
        {
            var activeSessions = await _sessionRepo.FindActiveByPlateAsync(plateNumber);
            if (activeSessions.Any())
            {
                throw new InvalidOperationException($"Xe {plateNumber} đang ở trong bãi, không thể check-in lại.");
            }

            Vehicle vehicle = CreateVehicle(vehicleType, plateNumber);
            bool isElectric = vehicle is ElectricCar || vehicle is ElectricMotorbike || vehicle is ElectricBicycle;

            var monthlyTicket = await _monthlyTicketRepo.FindActiveByPlateAsync(plateNumber);
            bool isMonthly = monthlyTicket != null;

            var zone = await _zoneRepo.FindSuitableZoneAsync(vehicleType, isElectric);
            if (zone == null)
            {
                throw new InvalidOperationException("Bãi xe đã hết chỗ hoặc không tìm thấy khu vực phù hợp.");
            }

            var sessionTicket = isMonthly
                ? new Ticket { TicketId = monthlyTicket.TicketId, IssueTime = DateTime.Now, GateId = gateId }
                : new Ticket { TicketId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), IssueTime = DateTime.Now, GateId = gateId };

            var session = new ParkingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                EntryTime = DateTime.Now,
                Vehicle = vehicle,
                Ticket = sessionTicket,
                Status = "Active",
                ParkingZoneId = zone.ZoneId
            };

            if (!isMonthly)
            {
                await _ticketRepo.AddAsync(sessionTicket);
            }

            zone.AddSession(session);
            await _sessionRepo.AddAsync(session);
            await _gateDevice.OpenGateAsync(gateId);

            return session;
        }

        // --- USE CASE: CHECK OUT (Xe ra - Tính tiền) ---
        public async Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId)
        {
            var session = await _sessionRepo.FindByTicketIdAsync(ticketIdOrPlate);
            if (session == null)
            {
                var sessions = await _sessionRepo.FindActiveByPlateAsync(ticketIdOrPlate);
                session = sessions.FirstOrDefault();
            }
            if (session == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin gửi xe.");
            }

            session.SetExitTime(DateTime.Now);

            bool isMonthly = session.Ticket.TicketId.StartsWith("M-");

            if (isMonthly)
            {
                session.FeeAmount = 0;
                session.Status = "Completed";
                await _sessionRepo.UpdateAsync(session);
                await _gateDevice.OpenGateAsync(gateId);
                return session;
            }

            var zone = await _zoneRepo.GetByIdAsync(session.ParkingZoneId);
            var policy = zone?.PricePolicy ?? new ParkingFeePolicy { PolicyId = "DEFAULT", Name = "Default Policy" };
            session.FeeAmount = policy.CalculateFee(session);
            session.Status = "PendingPayment";

            await _sessionRepo.UpdateAsync(session);
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