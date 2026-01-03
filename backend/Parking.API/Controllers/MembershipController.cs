using System;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;

        public MembershipController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var customer = new Customer { Name = request.Name, Phone = request.Phone, IdentityNumber = request.IdentityNumber };
                var vehicle = new Car(request.PlateNumber);

                var ticket = await _membershipService.RegisterMonthlyTicketAsync(customer, vehicle, "MONTHLY_PLAN");
                return Ok(ticket);
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
    }
}
