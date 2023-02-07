using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MeDirectCurrencyExchange.Api.Extensions;

public static class DistributedCacheExtension
{
    public static async Task<T?> GetFromCacheAsync<T>(this IDistributedCache distributedCache, string key)
    {
        string? cachedObject = await distributedCache.GetStringAsync(key);

        if (!string.IsNullOrWhiteSpace(cachedObject))
        {
            T? objectFromCache = JsonSerializer.Deserialize<T>(cachedObject, new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                IncludeFields = true,
            });
            return objectFromCache;
        }

        return default;
    }

    public static async Task SetInCacheAsync<U>(this IDistributedCache distributedCache, string key, U obj, int secondsToLive)
    {
        if (obj == null) return;

        string objSerialized = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            IncludeFields = true,
            
        });

        await distributedCache.SetStringAsync(key, objSerialized, new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(secondsToLive)
        });
    }

    public static void RemoveFromCache(this IDistributedCache distributedCache, string key)
    {
        distributedCache.Remove(key);
    }
}
