using System.Text.Json;
using StackExchange.Redis;

namespace ProductCatalogService.BLL.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task InvalidateAsync(string key);
        Task InvalidatePatternAsync(string pattern);
    }
}
