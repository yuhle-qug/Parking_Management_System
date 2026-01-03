using System.Threading.Tasks;
using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IUserRepository : IRepository<UserAccount>
    {
        Task<UserAccount> FindByUsernameAsync(string username);
    }
}
