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

	// [P3] Domain Services for CheckOut
	public interface IPricingService
	{
		Task<double> CalculateFeeAsync(ParkingSession session);
	}

	public interface IValidationService
	{
		// Validates request matches session (Plate, Card)
		void ValidateCheckOut(ParkingSession session, string plateNumber, string? cardId);
	}

	public interface ICheckOutService
	{
		Task<ParkingSession> CheckOutAsync(string ticketIdOrPlate, string gateId, string? plateNumber = null, string? cardId = null);
		Task<ParkingSession> ProcessLostTicketAsync(string plateNumber, string vehicleType, string gateId);
		Task<PaymentResult> ConfirmPaymentAsync(string sessionId, string transactionCode, bool success, string? providerLog = null, string? exitGateId = null);
	}

	// [P3] Membership Logic Split
	public interface ICustomerService
	{
		Task<Customer> GetOrCreateCustomerAsync(Customer customerInfo);
		Task<Customer?> GetCustomerByIdAsync(string customerId);
		Task<Customer?> GetCustomerByPhoneAsync(string phone);
		Task UpdateCustomerAsync(Customer customer);
	}
}
