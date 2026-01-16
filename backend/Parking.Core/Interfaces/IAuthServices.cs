using System.Collections.Generic;

namespace Parking.Core.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public interface IJwtService
    {
        string GenerateToken(string userId, string username, string role);
    }
}
