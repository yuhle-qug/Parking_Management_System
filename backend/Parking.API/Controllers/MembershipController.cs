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
        private readonly IMonthlyTicketRepository _monthlyTicketRepo;

        public MembershipController(IMembershipService membershipService, IMonthlyTicketRepository monthlyTicketRepo)
        {
            _membershipService = membershipService;
            _monthlyTicketRepo = monthlyTicketRepo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var customer = new Customer { Name = request.Name, Phone = request.Phone, IdentityNumber = request.IdentityNumber };
                var vehicle = CreateVehicle(request.VehicleType, request.PlateNumber);

                var ticket = await _membershipService.RegisterMonthlyTicketAsync(customer, vehicle, "MONTHLY_PLAN");
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // DELETE: api/Membership/tickets/{ticketId}
        [HttpDelete("tickets/{ticketId}")]
        public async Task<IActionResult> DeleteTicket(string ticketId)
        {
            await _monthlyTicketRepo.DeleteAsync(ticketId);
            return Ok(new { Message = "Deleted" });
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
    }

    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IdentityNumber { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }
    }
}
