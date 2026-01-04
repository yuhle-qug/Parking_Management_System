using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class UserRepository : BaseJsonRepository<UserAccount>, IUserRepository
    {
        public UserRepository(IHostEnvironment hostEnvironment) : base(hostEnvironment, "users.json") { }

        public async Task<UserAccount> FindByUsernameAsync(string username)
        {
            var users = await GetAllAsync();
            return users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
