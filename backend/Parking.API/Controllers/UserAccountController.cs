using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAccountController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UserAccountController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepo.FindByUsernameAsync(request.Username);

            if (user == null || user.PasswordHash != request.Password)
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }

            if (user.Status == "Locked")
            {
                return BadRequest(new { Message = "Tài khoản bị khóa" });
            }

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Permissions = new
                {
                    Report = user.CanAccessReports(),
                    ManageUser = user.CanManageUsers()
                }
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var existing = await _userRepo.FindByUsernameAsync(request.Username);
            if (existing != null) return BadRequest(new { Error = "Username đã tồn tại" });

            UserAccount newUser = request.Role?.ToUpper() == "ADMIN"
                ? new AdminAccount()
                : new AttendantAccount();

            newUser.UserId = Guid.NewGuid().ToString();
            newUser.Username = request.Username;
            newUser.PasswordHash = request.Password;
            newUser.Status = "Active";

            await _userRepo.AddAsync(newUser);
            return Ok(new { Message = "Tạo user thành công", UserId = newUser.UserId, Role = newUser.Role });
        }

        // [NEW] GET: api/UserAccount
        // API lấy danh sách nhân viên để hiển thị lên bảng Admin
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepo.GetAllAsync();
            // Ẩn mật khẩu trước khi trả về frontend vì lý do bảo mật
            var safeUsers = users.Select(u => new { 
                u.UserId, 
                u.Username, 
                u.Role, 
                u.Status 
            });
            return Ok(safeUsers);
        }

        // DELETE: api/UserAccount/{userId}
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound(new { Error = "Không tìm thấy user" });

            // Safety: don't allow deleting the default admin account
            if (user.Username?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { Error = "Không thể xóa tài khoản admin mặc định" });
            }

            await _userRepo.DeleteAsync(userId);
            return Ok(new { Message = "Xóa user thành công" });
        }

        // PATCH: api/UserAccount/{userId}/status
        [HttpPatch("{userId}/status")]
        public async Task<IActionResult> UpdateStatus(string userId, [FromBody] UpdateUserStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { Error = "Thiếu trạng thái" });
            }

            var normalized = request.Status.Trim();
            if (!normalized.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Trạng thái không hợp lệ (chỉ Active/Locked)" });
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound(new { Error = "Không tìm thấy user" });

            // Safety: don't allow locking the default admin account
            if (user.Username?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true &&
                normalized.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Error = "Không thể khóa tài khoản admin mặc định" });
            }

            user.Status = normalized.Equals("Locked", StringComparison.OrdinalIgnoreCase) ? "Locked" : "Active";
            await _userRepo.UpdateAsync(user);
            return Ok(new { Message = "Cập nhật trạng thái thành công", Status = user.Status });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

	public class UpdateUserStatusRequest
	{
		public string Status { get; set; }
	}
}
