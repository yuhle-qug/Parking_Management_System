using System;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // URL: api/Payment
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ICheckOutService _checkOutService;

        public PaymentController(IPaymentService paymentService, ICheckOutService checkOutService)
        {
            _paymentService = paymentService;
            _checkOutService = checkOutService;
        }

        [HttpPost]
        public async Task<IActionResult> PayAndExit([FromBody] PaymentRequest request)
        {
            try
            {
                var method = string.IsNullOrWhiteSpace(request.Method) ? "QR" : request.Method.Trim();

                // Chặn thanh toán tiền mặt theo yêu cầu thiết kế
                if (string.Equals(method, "cash", StringComparison.OrdinalIgnoreCase) || string.Equals(method, "cash/qr", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { Message = "Chỉ hỗ trợ thanh toán online/QR, không nhận tiền mặt." });
                }

                var result = await _paymentService.ProcessPaymentAsync(request.SessionId, request.Amount, method, request.ExitGateId, request.MaxRetry, request.TimeoutSeconds);
                if (result.Success)
                {
                    return Ok(new
                    {
                        Message = "Tạo mã QR thành công, chờ gateway xác nhận.",
                        Status = result.Status,
                        TransactionCode = result.TransactionCode,
                        Attempts = result.Attempts,
                        QrContent = result.QrContent,
                        ProviderLog = result.ProviderLog
                    });
                }

                return BadRequest(new { Message = result.Error ?? "Thanh toán thất bại.", Status = result.Status, TransactionCode = result.TransactionCode, Attempts = result.Attempts });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] PaymentCallbackRequest request)
        {
            try
            {
                var success = string.Equals(request.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                var result = await _checkOutService.ConfirmPaymentAsync(request.SessionId, request.TransactionCode, success, request.ProviderLog, request.ExitGateId);

                return Ok(new
                {
                    Message = "Đã ghi nhận log thanh toán từ gateway",
                    Status = result.Status,
                    TransactionCode = result.TransactionCode,
                    ProviderLog = result.ProviderLog
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelPaymentRequest request)
        {
            try
            {
                var result = await _paymentService.CancelPaymentAsync(request.SessionId, request.Reason ?? "User cancelled");
                return Ok(new { Message = "Đã hủy thanh toán", Status = result.Status, TransactionCode = result.TransactionCode, Error = result.Error });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class PaymentRequest
    {
        public string SessionId { get; set; }
        public double Amount { get; set; }
        public string? ExitGateId { get; set; }
        public string? Method { get; set; } = "QR";
        public int MaxRetry { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 5;
    }

    public class PaymentCallbackRequest
    {
        public string SessionId { get; set; }
        public string TransactionCode { get; set; }
        public string Status { get; set; }
        public string? ProviderLog { get; set; }
        public string? ExitGateId { get; set; }
    }

    public class CancelPaymentRequest
    {
        public string SessionId { get; set; }
        public string? Reason { get; set; }
    }
}
