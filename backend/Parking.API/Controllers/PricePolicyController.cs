using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricePolicyController : ControllerBase
    {
        private readonly IPricePolicyRepository _priceRepo;
        private readonly IParkingZoneRepository _zoneRepo;

        public PricePolicyController(IPricePolicyRepository priceRepo, IParkingZoneRepository zoneRepo)
        {
            _priceRepo = priceRepo;
            _zoneRepo = zoneRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _priceRepo.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{policyId}")]
        public async Task<IActionResult> Get(string policyId)
        {
            var policy = await _priceRepo.GetPolicyAsync(policyId);
            if (policy == null) return NotFound(new { Error = "Không tìm thấy policy" });
            return Ok(policy);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PricePolicy policy)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyId)) return BadRequest(new { Error = "PolicyId bắt buộc" });
            if (string.IsNullOrWhiteSpace(policy.Name)) return BadRequest(new { Error = "Name bắt buộc" });

            var exists = await _priceRepo.GetPolicyAsync(policy.PolicyId);
            if (exists != null) return Conflict(new { Error = "PolicyId đã tồn tại" });

            NormalizePolicy(policy);
            await _priceRepo.AddAsync(policy);
            return Ok(policy);
        }

        [HttpPut("{policyId}")]
        public async Task<IActionResult> Update(string policyId, [FromBody] PricePolicy policy)
        {
            var existing = await _priceRepo.GetPolicyAsync(policyId);
            if (existing == null) return NotFound(new { Error = "Không tìm thấy policy" });

            policy.PolicyId = policyId;
            NormalizePolicy(policy);
            await _priceRepo.UpdateAsync(policy);
            return Ok(policy);
        }

        [HttpDelete("{policyId}")]
        public async Task<IActionResult> Delete(string policyId)
        {
            var existing = await _priceRepo.GetPolicyAsync(policyId);
            if (existing == null) return NotFound(new { Error = "Không tìm thấy policy" });

            var zones = await _zoneRepo.GetAllAsync();
            var inUse = zones.Any(z => string.Equals(z.PricePolicyId, policyId, StringComparison.OrdinalIgnoreCase));
            if (inUse)
            {
                return BadRequest(new { Error = "Policy đang được sử dụng bởi zone, không thể xóa" });
            }

            await _priceRepo.DeleteAsync(policyId);
            return Ok(new { Message = "Đã xóa" });
        }

        private static void NormalizePolicy(PricePolicy policy)
        {
            if (policy.RatePerHour < 0) policy.RatePerHour = 0;
            if (policy.OvernightSurcharge < 0) policy.OvernightSurcharge = 0;
            if (policy.DailyMax < 0) policy.DailyMax = 0;
            if (policy.LostTicketFee < 0) policy.LostTicketFee = 0;
            if (policy.PeakRanges == null) policy.PeakRanges = new();
            policy.VehicleType = string.IsNullOrWhiteSpace(policy.VehicleType) ? "CAR" : policy.VehicleType.ToUpperInvariant();
        }
    }
}
