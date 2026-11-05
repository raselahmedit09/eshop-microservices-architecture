namespace Basket.API.Data
{
    // Bound from the "CacheSettings" section of appsettings.json
    public class CacheSettings
    {
        // A basket stays in Redis while it is used; it is evicted after this many minutes
        // without a read or write. PostgreSQL still keeps it, so eviction only costs one DB read.
        public int SlidingExpirationMinutes { get; set; } = 30;

        // Maximum time a basket stays in Redis, even if it keeps being read
        public int AbsoluteExpirationMinutes { get; set; } = 120;
    }
}
