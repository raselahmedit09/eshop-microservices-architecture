using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Basket.API.Data
{
    public class CachedBasketRepository(
        IBasketRepository basketRepository,
        IDistributedCache distributedCache,
        IOptions<CacheSettings> cacheSettings,
        ILogger<CachedBasketRepository> logger) : IBasketRepository
    {
        private readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(cacheSettings.Value.SlidingExpirationMinutes),
            // Upper bound, so a basket that is read constantly still reloads from PostgreSQL eventually
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheSettings.Value.AbsoluteExpirationMinutes)
        };

        // Prefixed so basket keys cannot collide with other keys in the same Redis
        private static string CacheKey(string userName) => $"basket:{userName}";

        public async Task<ShoppingCart> GetBasket(string userName, CancellationToken cancellationToken = default)
        {
            var cachedBasket = await TryCacheAsync(() => distributedCache.GetStringAsync(CacheKey(userName), cancellationToken));
            if (!string.IsNullOrEmpty(cachedBasket))
                return JsonSerializer.Deserialize<ShoppingCart>(cachedBasket)!;

            var basket = await basketRepository.GetBasket(userName, cancellationToken);
            await TryCacheAsync(() => distributedCache.SetStringAsync(CacheKey(userName), JsonSerializer.Serialize(basket), _cacheOptions, cancellationToken));
            return basket;
        }

        public async Task<ShoppingCart> StoreBasket(ShoppingCart basket, CancellationToken cancellationToken = default)
        {
            await basketRepository.StoreBasket(basket, cancellationToken);

            // Remove instead of overwrite: the next read loads the basket from PostgreSQL, so a slow
            // concurrent read cannot leave an older basket in Redis on top of this one
            await TryCacheAsync(() => distributedCache.RemoveAsync(CacheKey(basket.UserName), cancellationToken));

            return basket;
        }

        public async Task<bool> DeleteBasket(string userName, CancellationToken cancellationToken = default)
        {
            await basketRepository.DeleteBasket(userName, cancellationToken);

            await TryCacheAsync(() => distributedCache.RemoveAsync(CacheKey(userName), cancellationToken));

            return true;
        }

        // Redis is only a cache: when it is down, log it and let the request continue against PostgreSQL
        private async Task<T?> TryCacheAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
            {
                logger.LogWarning(ex, "Redis unavailable, falling back to the database");
                return default;
            }
        }

        private Task TryCacheAsync(Func<Task> action) =>
            TryCacheAsync(async () => { await action(); return true; });
    }
}
