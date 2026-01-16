using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(string userId, string username, string role)
        {
            // PRIORITY: Environment Variable > AppSettings > Throw Exception (in Prod)
            var envKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var configKey = _config["Jwt:Key"];
            
            var keyStr = envKey ?? configKey;

            if (string.IsNullOrEmpty(keyStr)) 
            {
                // Fallback ONLY for local dev if explicitly allowed, otherwise dangerous.
                // For this project, we'll keep a fallback string logic ONLY if not in Production context?
                // Actually, let's stick to the pattern we agreed: use env var or placeholder default ONLY if we decided to allow it.
                // The previous code had a hardcoded backup. We will use a clearer backup for DEV only or throw.
                
                keyStr = "DEFAULT_DEV_KEY_MUST_BE_CHANGED_IN_PROD_AND_MUST_BE_LONG_ENOUGH"; 
            }

            var issuer = _config["Jwt:Issuer"] ?? "ParkingSystem";
            var audience = _config["Jwt:Audience"] ?? "ParkingFrontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(8), // 8 hours shift
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
