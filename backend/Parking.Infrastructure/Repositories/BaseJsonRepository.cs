using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Parking.Core.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories
{
	// Base repository that persists each operation to a JSON file on disk.
	public abstract class BaseJsonRepository<T> : IRepository<T> where T : class
	{
		protected readonly string _filePath;

		protected BaseJsonRepository(IHostEnvironment hostEnvironment, string fileName)
		{
			_filePath = ResolveDataStorePath(hostEnvironment.ContentRootPath, fileName);
		}

		protected BaseJsonRepository(string fileName)
		{
			_filePath = ResolveDataStorePath(AppDomain.CurrentDomain.BaseDirectory, fileName);
		}

		private static string ResolveDataStorePath(string contentRootPath, string fileName)
		{
			var candidates = new[]
			{
				Path.Combine(contentRootPath, "DataStore", fileName),
				Path.Combine(contentRootPath, "backend", "Parking.API", "DataStore", fileName),
				Path.Combine(AppContext.BaseDirectory, "DataStore", fileName)
			};

			foreach (var path in candidates)
			{
				var directory = Path.GetDirectoryName(path);
				if (File.Exists(path) || (directory != null && Directory.Exists(directory)))
				{
					return path;
				}
			}

			// Fallback to the first candidate; WriteListAsync will create the directory if needed.
			return candidates[0];
		}

		protected string? GetId(T entity)
		{
			var prop = entity.GetType().GetProperties()
				.FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && p.Name.Length > 2);

			return prop?.GetValue(entity)?.ToString();
		}


		public virtual async Task<IEnumerable<T>> GetAllAsync()
		{
			return await JsonFileHelper.ReadListAsync<T>(_filePath);
		}

		public virtual async Task<T?> GetByIdAsync(string id)
		{
			// Read is also locked inside ReadListAsync, so it's safe from partial writes,
			// though it might see slightly stale data if a write is pending (standard behavior).
			var list = await GetAllAsync();
			return list.FirstOrDefault(item => GetId(item) == id);
		}

		public virtual async Task AddAsync(T entity)
		{
			await JsonFileHelper.ExecuteInTransactionAsync<T>(_filePath, list =>
			{
				list.Add(entity);
				return true; // Save changes
			});
		}

		public virtual async Task UpdateAsync(T entity)
		{
			await JsonFileHelper.ExecuteInTransactionAsync<T>(_filePath, list =>
			{
				var id = GetId(entity);
				var index = list.FindIndex(item => GetId(item) == id);

				if (index != -1)
				{
					list[index] = entity;
					return true; // Save changes
				}
				return false; // No changes
			});
		}

		public virtual async Task DeleteAsync(string id)
		{
			await JsonFileHelper.ExecuteInTransactionAsync<T>(_filePath, list =>
			{
				var removed = list.RemoveAll(item => GetId(item) == id);
				return removed > 0; // Save if anything was removed
			});
		}
	}
}

