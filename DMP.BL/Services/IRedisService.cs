using DMP.BL.Models;

namespace DMP.BL.Services;

public interface IRedisService
{
    Task<T?> GetCacheValueAsync<T>(string key);

    Task SetProductAsync(Product product, string rootCategoryId);

    /// <summary>
    /// Pops the next system notification, waiting up to a couple of seconds; returns <c>null</c> when the queue is empty.
    /// </summary>
    Task<string?> GetSysNotificationAsync();
}
