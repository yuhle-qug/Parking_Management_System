using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
	public interface IParkingService
	{
		Task<ParkingSession> CheckInAsync(string plateNumber, string vehicleType, string gateId, string? cardId = null);
		Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId, string? plateNumber = null, string? cardId = null);
		Task<ParkingSession> ProcessLostTicketAsync(string plateNumber, string vehicleType, string gateId);
		Task<PaymentResult> ConfirmPaymentAsync(string sessionId, string transactionCode, bool success, string? providerLog = null, string? exitGateId = null);
	}

	public interface IPaymentService
	{
		Task<PaymentResult> ProcessPaymentAsync(string sessionId, double amount, string method, string? exitGateId = null, int maxRetry = 3, int timeoutSeconds = 5);
		Task<PaymentResult> CancelPaymentAsync(string sessionId, string reason = "User cancelled");
	}

	public interface IMembershipService
	{
		Task<MonthlyTicket> RegisterMonthlyTicketAsync(Customer customerInfo, Vehicle vehicle, string planId, int months = 1, string? brand = null, string? color = null, string? performedBy = null);
		Task<MonthlyTicket> ExtendMonthlyTicketAsync(string ticketId, int months, string performedBy, string? note = null);
		Task<MonthlyTicket> CancelMonthlyTicketAsync(string ticketId, string performedBy, bool isAdmin, string? note = null);
		Task<MonthlyTicket> ApproveCancellationAsync(string ticketId, string adminId);
		
		// [NEW] Thêm 2 hàm lấy dữ liệu
		Task<IEnumerable<MonthlyTicketDto>> GetAllTicketsAsync();
		Task<IEnumerable<MembershipPolicy>> GetAllPoliciesAsync();
		Task<IEnumerable<MembershipHistory>> GetHistoryAsync(string ticketId);
	}

	// External device abstraction for opening gates.
	public interface IGateDevice
	{
		Task OpenGateAsync(string gateId);
	}

	// Payment gateway adapter contract.
	public interface IPaymentGateway
	{
		Task<PaymentGatewayResult> RequestPaymentAsync(double amount, string orderInfo, CancellationToken cancellationToken = default);
	}

	public interface IPlateRecognitionClient
	{
		Task<PlateRecognitionResult> RecognizeAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);
	}

	public interface ITicketTemplateService
	{
		TicketPrintResult RenderHtml(TicketPrintData data);
	}
}
