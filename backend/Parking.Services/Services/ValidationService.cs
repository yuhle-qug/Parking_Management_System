using System;
using System.Linq;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Parking.Services.Services
{
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        public void ValidateCheckOut(ParkingSession session, string plateNumber, string? cardId)
        {
            if (string.IsNullOrWhiteSpace(plateNumber))
            {
                throw new InvalidOperationException("Biển số bắt buộc khi cho xe ra.");
            }

            // Plate Normalization Helper
            string Normalize(string input) 
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                return new string(input.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
            }

            var normalizedInput = Normalize(plateNumber);
            var normalizedStored = Normalize(session.Vehicle.LicensePlate?.Value ?? "");

            // Validation 1: Plate Match
            if (normalizedInput != normalizedStored)
            {
                _logger.LogWarning("Biển số không khớp ticket {TicketId}: nhập {InputPlate}, lưu {StoredPlate}", 
                    session.Ticket?.TicketId, normalizedInput, normalizedStored);
                throw new InvalidOperationException("Biển số không khớp, cần xác minh hoặc xử lý mất vé.");
            }

            // Validation 2: Card Match (if applicable)
            var storedCard = session.Ticket?.CardId ?? session.CardId;
            if (!string.IsNullOrWhiteSpace(storedCard))
            {
                var normInputCard = (cardId ?? string.Empty).Trim().ToUpperInvariant();
                var normStoredCard = storedCard.Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(normInputCard) || normInputCard != normStoredCard)
                {
                    _logger.LogWarning("CardId không khớp ticket {TicketId}: nhập {InputCard}, lưu {StoredCard}", 
                        session.Ticket?.TicketId, normInputCard, normStoredCard);
                    throw new InvalidOperationException("Thẻ không khớp, cần xử lý mất vé.");
                }
            }

            // Validation 3: Monthly Ticket specific
            bool isMonthly = session.Ticket != null && session.Ticket.TicketId.StartsWith("M-");
            if (isMonthly)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    throw new InvalidOperationException("Vé tháng cần quẹt thẻ (cardId) khi ra.");
                }
            }
            else
            {
                // Verify TicketId for daily ticket? 
                // Currently CheckOutService fetches by ticketIdOrPlate, so mismatch here is unlikely if fetched by ticketId.
                // But if fetched by Plate, we might want to ensure TicketId matches if provided.
                // Skipped to keep simple matching P3 scope.
            }
        }
    }
}
