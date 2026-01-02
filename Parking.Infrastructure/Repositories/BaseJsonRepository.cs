using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Repositories
{
	// Minimal in-memory base repository; fileName kept for future JSON persistence.
	public abstract class BaseJsonRepository<T> : IRepository<T> where T : class
	{
		private static readonly List<T> Items = new();
		private readonly string _fileName;

		protected BaseJsonRepository(string fileName)
		{
			_fileName = fileName;
		}

		public virtual Task<IEnumerable<T>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<T>>(Items.ToList());
		}

		public virtual Task<T?> GetByIdAsync(string id)
		{
			var item = Items.FirstOrDefault(x => GetId(x) == id);
			return Task.FromResult(item);
		}

		public virtual Task AddAsync(T entity)
		{
			Items.Add(entity);
			return Task.CompletedTask;
		}

		public virtual Task UpdateAsync(T entity)
		{
			var id = GetId(entity);
			if (id == null) return Task.CompletedTask;

			var idx = Items.FindIndex(x => GetId(x) == id);
			if (idx >= 0)
			{
				Items[idx] = entity;
			}
			return Task.CompletedTask;
		}

		private static string? GetId(T entity)
		{
			// Look for an Id-like string property (Id, <Type>Id, or ending with "Id").
			var prop = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(p => p.PropertyType == typeof(string) &&
									 (p.Name == "Id" || p.Name.EndsWith("Id")));
			return prop?.GetValue(entity) as string;
		}
	}
}
