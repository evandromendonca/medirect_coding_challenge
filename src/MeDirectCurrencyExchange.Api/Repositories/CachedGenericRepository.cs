using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq.Expressions;
using System.Text.Json;

namespace MeDirectCurrencyExchange.Api.Repositories
{
    public class CachedGenericRepository<T> : IGenericRepository<T> where T : BaseClass
    {
        private readonly IGenericRepository<T> _decoratedGenericRepository;
        private readonly IDistributedCache _distributedCache;
        public CachedGenericRepository(IGenericRepository<T> decoratedGenericRepository, IDistributedCache distributedCache)
        {
            _decoratedGenericRepository = decoratedGenericRepository;
            _distributedCache = distributedCache;
        }

        public virtual async Task AddAsync(T entity)
        {
            await _decoratedGenericRepository.AddAsync(entity);

            string key = $"{nameof(T)}-id-{entity.Id}";
            await SetInCacheAsync(key, entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _decoratedGenericRepository.AddRangeAsync(entities);
        }

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
        {
            return _decoratedGenericRepository.FindAsync(expression);
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return _decoratedGenericRepository.GetAllAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            string key = $"{nameof(T)}-id-{id}";

            T obj = await GetFromCacheAsync<T>(key);

            if (obj == null)
            {
                obj = await _decoratedGenericRepository.GetByIdAsync(id);
                await SetInCacheAsync(key, obj);
            }

            return obj;
        }

        public void Remove(T entity)
        {
            _decoratedGenericRepository.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _decoratedGenericRepository.RemoveRange(entities);
        }

        #region private methods

        protected async Task<U> GetFromCacheAsync<U>(string key)
        {
            string? cachedObject = await _distributedCache.GetStringAsync(key);

            if (!string.IsNullOrWhiteSpace(cachedObject))
            {
                U objectFromCache = JsonSerializer.Deserialize<U>(cachedObject);
                return objectFromCache;
            }

            return default;
        }

        protected async Task SetInCacheAsync<U>(string key, U obj, int secondsTtl = 5 * 60)
        {
            if (obj == null) return;

            string objSerialized = JsonSerializer.Serialize(obj);
            await _distributedCache.SetStringAsync(key, objSerialized, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(secondsTtl)
            });
        }

        protected void RemoveFromCache(string key)
        {
            _distributedCache.Remove(key);
        }

        #endregion
    }
}
