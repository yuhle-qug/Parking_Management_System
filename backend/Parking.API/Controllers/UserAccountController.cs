using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Entities;
using Parking.Core.Interfaces;
using Parking.Services.Services;

namespace Parking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAccountController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtService _jwtService;
        private readonly IAuditService _auditService;

        public UserAccountController(
            IUserRepository userRepo, 
            IPasswordHasher hasher, 
            IJwtService jwtService,
            IAuditService auditService)
        {
            _userRepo = userRepo;
            _hasher = hasher;
            _jwtService = jwtService;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            Console.WriteLine($"[DEBUG] Login attempt for username: '{request.Username}' with password: '{request.Password}'");
            var user = await _userRepo.FindByUsernameAsync(request.Username);

            if (user == null) {
                 Console.WriteLine($"[DEBUG] User '{request.Username}' NOT FOUND in repository.");
            } else {
                 Console.WriteLine($"[DEBUG] User '{request.Username}' FOUND. Hash: '{user.PasswordHash}', Role: '{user.Role}'");
            }

            if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                Console.WriteLine("[DEBUG] Password verification failed.");
                await _auditService.LogAsync("Login", request.Username, null, "Failed login attempt", success: false);
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }

            if (user.Status == "Locked")
            {
                await _auditService.LogAsync("Login", request.Username, null, "Locked account login attempt", success: false);
                return BadRequest(new { Message = "Tài khoản bị khóa" });
            }

            // Lazy migration: if verifying plain text passed (handled inside VerifyPassword) but it wasn't hashed yet,
            // we might want to re-hash. But for simplicity, we assume VerifyPassword handles both or we just proceed.
            // A more robust lazy migration would be: if (Verify(plain)) { user.PasswordHash = Hash(plain); await _userRepo.UpdateAsync(user); }
            // Given the BcryptPasswordHasher implementation I wrote supports plain text check fallback, migration is implicit but doesn't auto-update DB.
            
            var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Role);
            await _auditService.LogAsync("Login", user.Username, user.UserId, "Login success");

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Token = token,
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
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var existing = await _userRepo.FindByUsernameAsync(request.Username);
            if (existing != null) return BadRequest(new { Error = "Username đã tồn tại" });

            UserAccount newUser = request.Role?.ToUpper() == "ADMIN"
                ? new AdminAccount()
                : new AttendantAccount();

            newUser.UserId = Guid.NewGuid().ToString();
            newUser.Username = request.Username;
            newUser.PasswordHash = _hasher.HashPassword(request.Password); // Hash password
            newUser.Status = "Active";

            await _userRepo.AddAsync(newUser);
            await _auditService.LogAsync("CreateUser", User.Identity?.Name ?? "Admin", newUser.UserId, $"Created user {newUser.Username} as {newUser.Role}");
            
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

        // [NEW] PUT: api/UserAccount/{userId}
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound(new { Error = "Không tìm thấy user" });

            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName;
            if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.Phone)) user.Phone = request.Phone;
            
            // Password change logic (optional - usually separate endpoint but included for simplicity)
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = _hasher.HashPassword(request.Password);
            }

            await _userRepo.UpdateAsync(user);
            await _auditService.LogAsync("UpdateUser", User.Identity?.Name ?? "Admin", user.UserId, $"Updated info user {user.Username}");

            return Ok(new { Message = "Cập nhật thành công", User = new { user.UserId, user.Username, user.FullName, user.Email, user.Phone } });
        }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
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
