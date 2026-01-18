using System.Threading.Tasks;

namespace Parking.Core.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for managing database transactions
    /// Ensures atomic operations across multiple repository calls
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Begin a new database transaction
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackAsync();

        /// <summary>
        /// Save all pending changes to the database
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
