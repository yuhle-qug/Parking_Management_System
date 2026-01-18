using System;
using System.Threading;
using System.Threading.Tasks;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Persistence
{
    /// <summary>
    /// Unit of Work implementation for JSON-based storage
    /// Provides transaction-like semantics using SemaphoreSlim for coordination
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool _isInTransaction = false;

        public async Task BeginTransactionAsync()
        {
            if (_isInTransaction)
            {
                throw new InvalidOperationException("Transaction already started.");
            }

            await _lock.WaitAsync();
            _isInTransaction = true;
        }

        public Task CommitAsync()
        {
            if (!_isInTransaction)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                // In JSON storage, commits are implicit (files are written immediately)
                // This just releases the lock
                return Task.CompletedTask;
            }
            finally
            {
                _isInTransaction = false;
                _lock.Release();
            }
        }

        public Task RollbackAsync()
        {
            if (!_isInTransaction)
            {
                throw new InvalidOperationException("No active transaction to rollback.");
            }

            // In JSON storage, rollback is not directly supported
            // This implementation just releases the lock
            _isInTransaction = false;
            _lock.Release();
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync()
        {
            // In JSON storage, changes are saved immediately by repositories
            // Return 1 to indicate success
            return Task.FromResult(1);
        }
    }
}
