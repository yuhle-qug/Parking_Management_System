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

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> PayAndExit([FromBody] PaymentRequest request)
        {
            try
            {
                bool success = await _paymentService.ProcessPaymentAsync(request.SessionId, request.Amount, "Cash/QR");
                if (success)
                {
                    return Ok(new { Message = "Thanh toán thành công. Cổng đang mở.", Status = "Completed" });
                }

                return BadRequest(new { Message = "Thanh toán thất bại." });
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
    }
}
