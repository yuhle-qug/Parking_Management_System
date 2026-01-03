using System;
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
}
