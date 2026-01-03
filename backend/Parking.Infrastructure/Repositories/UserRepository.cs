using System.Linq;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
    public class UserRepository : BaseJsonRepository<UserAccount>, IUserRepository
    {
        public UserRepository() : base("users.json") { }

        public async Task<UserAccount> FindByUsernameAsync(string username)
        {
            var users = await GetAllAsync();
            return users.FirstOrDefault(u => u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
