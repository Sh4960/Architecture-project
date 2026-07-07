using System.Text.Json;
using StackExchange.Redis;
using ProductCatalogService.BLL.Interfaces;

namespace ProductCatalogService.BLL
{
    public class RedisCache : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCache> _logger;
        private readonly IDatabase _db;

        public RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (value.IsNull)
                {
                    _logger.LogInformation("Cache MISS for key: {Key}", key);
                    return default;
                }

                _logger.LogInformation("Cache HIT for key: {Key}", key);
                var deserializedValue = JsonSerializer.Deserialize<T>(value.ToString());
                return deserializedValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting value from cache for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, serializedValue, expiration);
                _logger.LogInformation("Cache SET for key: {Key} with expiration {Expiration:HH:mm:ss}", key, expiration ?? TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting value in cache for key: {Key}", key);
            }
        }

        public async Task InvalidateAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
                _logger.LogInformation("Cache INVALIDATED for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for key: {Key}", key);
            }
        }

        public async Task InvalidatePatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"*{pattern}*");
                foreach (var key in keys)
                {
                    await _db.KeyDeleteAsync(key);
                }
                _logger.LogInformation("Cache INVALIDATED for pattern: {Pattern} ({Count} keys removed)", pattern, keys.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            }
        }
    }
}
