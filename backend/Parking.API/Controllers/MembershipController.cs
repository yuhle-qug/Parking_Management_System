using System;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using System.Threading.Tasks;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IMembershipPolicyRepository _policyRepo;

        public MembershipController(IMembershipService membershipService, IMonthlyTicketRepository monthlyTicketRepo, IMembershipPolicyRepository policyRepo, IPaymentGateway paymentGateway)
        {
            _membershipService = membershipService;
            _monthlyTicketRepo = monthlyTicketRepo;
            _policyRepo = policyRepo;
            _paymentGateway = paymentGateway;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PlanId))
                {
                    return BadRequest(new { Error = "Thiếu PlanId" });
                }

                if (request.Months <= 0) request.Months = 1;

                var customer = new Customer { Name = request.Name, Phone = request.Phone, IdentityNumber = request.IdentityNumber };
                var vehicle = CreateVehicle(request.VehicleType, request.PlateNumber);

                string performedBy = User.Identity?.Name ?? "staff";
                var ticket = await _membershipService.RegisterMonthlyTicketAsync(customer, vehicle, request.PlanId, request.Months, request.VehicleBrand, request.VehicleColor, performedBy);

                var orderInfo = $"Monthly ticket {ticket.TicketId} - Plate {vehicle.LicensePlate}";
                var gatewayResult = await _paymentGateway.RequestPaymentAsync(ticket.MonthlyFee, orderInfo);

                if (gatewayResult == null || !gatewayResult.Accepted)
                {
                    ticket.SetPaymentFailed(gatewayResult?.Error ?? "Gateway từ chối tạo QR");
                    await _monthlyTicketRepo.UpdateAsync(ticket);
                    return BadRequest(new { Error = ticket.ProviderLog });
                }

                ticket.SetPendingPayment(
                    gatewayResult.TransactionCode,
                    string.IsNullOrWhiteSpace(gatewayResult.PaymentUrl) ? gatewayResult.QrContent : gatewayResult.PaymentUrl,
                    gatewayResult.ProviderMessage
                );
                // ticket.QrContent and ProviderLog are already set in SetPendingPayment

                await _monthlyTicketRepo.UpdateAsync(ticket);

                return Ok(new
                {
                    Ticket = ticket,
                    Payment = new
                    {
                        Amount = ticket.MonthlyFee,
                        TransactionCode = ticket.TransactionCode,
                        QrContent = ticket.QrContent,
                        Status = "PendingPayment",
                        ProviderLog = ticket.ProviderLog
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("tickets/{ticketId}/extend")]
        public async Task<IActionResult> ExtendTicket(string ticketId, [FromBody] ExtendMembershipRequest request)
        {
            try
            {
                if (request.Months <= 0) request.Months = 1;

                string performedBy = User.Identity?.Name ?? request.PerformedBy ?? "staff";
                var ticket = await _membershipService.ExtendMonthlyTicketAsync(ticketId, request.Months, performedBy, request.Note);

                var orderInfo = $"Extend monthly ticket {ticket.TicketId} - Plate {ticket.VehiclePlate}";
                var gatewayResult = await _paymentGateway.RequestPaymentAsync(ticket.MonthlyFee, orderInfo);

                if (gatewayResult == null || !gatewayResult.Accepted)
                {
                    ticket.SetPaymentFailed(gatewayResult?.Error ?? "Gateway từ chối tạo QR");
                    await _monthlyTicketRepo.UpdateAsync(ticket);
                    return BadRequest(new { Error = ticket.ProviderLog });
                }

                ticket.SetPendingPayment(
                    gatewayResult.TransactionCode,
                    string.IsNullOrWhiteSpace(gatewayResult.PaymentUrl) ? gatewayResult.QrContent : gatewayResult.PaymentUrl,
                    gatewayResult.ProviderMessage
                );
                // ticket.QrContent and ProviderLog are already set in SetPendingPayment

                await _monthlyTicketRepo.UpdateAsync(ticket);

                return Ok(new
                {
                    Ticket = ticket,
                    Payment = new
                    {
                        Amount = ticket.MonthlyFee,
                        TransactionCode = ticket.TransactionCode,
                        QrContent = ticket.QrContent,
                        Status = "PendingPayment",
                        ProviderLog = ticket.ProviderLog
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmMembershipPaymentRequest request)
        {
            var ticket = await _monthlyTicketRepo.GetByIdAsync(request.TicketId);
            if (ticket == null)
            {
                return NotFound(new { Error = "Không tìm thấy vé tháng" });
            }

            var success = string.Equals(request.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                ticket.TransactionCode = string.IsNullOrWhiteSpace(request.TransactionCode) 
                    ? ticket.TransactionCode 
                    : request.TransactionCode;
                ticket.Activate();
            }
            else
            {
                ticket.SetPaymentFailed(request.ProviderLog ?? "User confirm failed");
            }

            await _monthlyTicketRepo.UpdateAsync(ticket);

            return Ok(new
            {
                Message = "Đã ghi nhận thanh toán vé tháng",
                TicketId = ticket.TicketId,
                Status = ticket.Status,
                TransactionCode = request.TransactionCode,
                ProviderLog = request.ProviderLog
            });
        }

        [HttpPost("payment-callback")]
        public async Task<IActionResult> PaymentCallback([FromBody] MembershipPaymentCallbackRequest request)
        {
            var ticket = await _monthlyTicketRepo.GetByIdAsync(request.TicketId);
            if (ticket == null)
            {
                return NotFound(new { Error = "Không tìm thấy vé tháng" });
            }

            var success = string.Equals(request.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

            if (success)
            {
                ticket.TransactionCode = string.IsNullOrWhiteSpace(request.TransactionCode) 
                    ? ticket.TransactionCode 
                    : request.TransactionCode;
                ticket.Activate();
            }
            else
            {
                ticket.SetPaymentFailed(request.ProviderLog ?? "Callback failed");
            }

            await _monthlyTicketRepo.UpdateAsync(ticket);

            return Ok(new
            {
                Message = "Đã ghi nhận callback thanh toán vé tháng",
                Ticket = ticket
            });
        }

        // DELETE: api/Membership/tickets/{ticketId}
        [HttpDelete("tickets/{ticketId}")]
        public async Task<IActionResult> DeleteTicket(string ticketId)
        {
            return BadRequest(new { Error = "Xóa vé tháng bị vô hiệu, hãy dùng hủy (cancel)." });
        }

        [HttpPost("tickets/{ticketId}/cancel")]
        public async Task<IActionResult> CancelTicket(string ticketId, [FromBody] CancelMembershipRequest request)
        {
            try
            {
                bool isAdmin = User.IsInRole("ADMIN");
                string performedBy = User.Identity?.Name ?? request.PerformedBy ?? "staff";

                var ticket = await _membershipService.CancelMonthlyTicketAsync(ticketId, performedBy, isAdmin, request.Note);
                
                string msg = isAdmin ? "Đã hủy vé tháng" : "Đã gửi yêu cầu hủy (chờ Admin duyệt)";
                return Ok(new { Message = msg, Ticket = ticket });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("tickets/{ticketId}/history")]
        public async Task<IActionResult> GetHistory(string ticketId)
        {
            var history = await _membershipService.GetHistoryAsync(ticketId);
            return Ok(history);
        }

        // [NEW] GET: api/Membership/tickets
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets()
        {
            var tickets = await _membershipService.GetAllTicketsAsync();
            return Ok(tickets);
        }

        // [NEW] GET: api/Membership/policies
        [HttpGet("policies")]
        public async Task<IActionResult> GetAllPolicies()
        {
            var policies = await _membershipService.GetAllPoliciesAsync();
            return Ok(policies);
        }

        [HttpPost("policies")]
        public async Task<IActionResult> CreatePolicy([FromBody] MembershipPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyId)) return BadRequest(new { Error = "PolicyId bắt buộc" });
            if (string.IsNullOrWhiteSpace(policy.VehicleType)) return BadRequest(new { Error = "VehicleType bắt buộc" });
            if (policy.MonthlyPrice <= 0) return BadRequest(new { Error = "MonthlyPrice phải > 0" });

            var existing = await _policyRepo.GetPolicyAsync(policy.PolicyId);
            if (existing != null) return Conflict(new { Error = "PolicyId đã tồn tại" });

            policy.VehicleType = policy.VehicleType.ToUpperInvariant();
            await _policyRepo.AddAsync(policy);
            return Ok(policy);
        }

        [HttpPut("policies/{policyId}")]
        public async Task<IActionResult> UpdatePolicy(string policyId, [FromBody] MembershipPolicy policy)
        {
            var existing = await _policyRepo.GetPolicyAsync(policyId);
            if (existing == null) return NotFound(new { Error = "Không tìm thấy policy" });

            if (policy.MonthlyPrice <= 0) return BadRequest(new { Error = "MonthlyPrice phải > 0" });
            policy.PolicyId = policyId;
            policy.VehicleType = string.IsNullOrWhiteSpace(policy.VehicleType) ? existing.VehicleType : policy.VehicleType.ToUpperInvariant();

            await _policyRepo.UpdateAsync(policy);
            return Ok(policy);
        }

        [HttpDelete("policies/{policyId}")]
        public async Task<IActionResult> DeletePolicy(string policyId)
        {
            var existing = await _policyRepo.GetPolicyAsync(policyId);
            if (existing == null) return NotFound(new { Error = "Không tìm thấy policy" });

            await _policyRepo.DeleteAsync(policyId);
            return Ok(new { Message = "Đã xóa" });
        }

        private static Vehicle CreateVehicle(string type, string plate)
        {
            var t = (type ?? string.Empty).Trim().ToUpperInvariant();
            var p = (plate ?? string.Empty).Trim().ToUpperInvariant();
            return t switch
            {
                "CAR" => new Car(p),
                "ELECTRIC_CAR" => new ElectricCar(p),
                "MOTORBIKE" => new Motorbike(p),
                "ELECTRIC_MOTORBIKE" => new ElectricMotorbike(p),
                "BICYCLE" => new Bicycle(p),
                _ => new Car(p)
            };
        }

        [HttpPost("tickets/{ticketId}/approve-cancel")]
        // [Authorize(Roles = "ADMIN")] // Uncomment when Auth is fully active
        public async Task<IActionResult> ApproveCancelTicket(string ticketId)
        {
            try
            {
                // Simulation: Check if user is admin (In real app, rely on [Authorize])
                // if (!User.IsInRole("ADMIN")) return Forbid();
                
                string adminId = User.Identity?.Name ?? "admin";
                var ticket = await _membershipService.ApproveCancellationAsync(ticketId, adminId);
                return Ok(new { Message = "Đã duyệt hủy vé tháng", Ticket = ticket });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IdentityNumber { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
        public string PlanId { get; set; }
        public int Months { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleColor { get; set; }
    }

    public class ConfirmMembershipPaymentRequest
    {
        public string TicketId { get; set; }
        public string Status { get; set; } = "SUCCESS"; // SUCCESS/FAILED
        public string TransactionCode { get; set; }
        public string? ProviderLog { get; set; }
    }

    public class MembershipPaymentCallbackRequest
    {
        public string TicketId { get; set; }
        public string TransactionCode { get; set; }
        public string Status { get; set; }
        public string? ProviderLog { get; set; }
    }

    public class ExtendMembershipRequest
    {
        public int Months { get; set; }
        public string? PerformedBy { get; set; }
        public string? Note { get; set; }
    }

    public class CancelMembershipRequest
    {
        public string? PerformedBy { get; set; }
        public string? Note { get; set; }
    }
}
