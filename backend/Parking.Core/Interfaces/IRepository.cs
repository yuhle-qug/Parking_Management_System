using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parking.Core.Interfaces
{
	// Generic repository contract for simple CRUD-style access.
	public interface IRepository<T>
	{
		Task<IEnumerable<T>> GetAllAsync();
		Task<T?> GetByIdAsync(string id);
		Task AddAsync(T entity);
		Task UpdateAsync(T entity);
		Task DeleteAsync(string id);
	}
}
