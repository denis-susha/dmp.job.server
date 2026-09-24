using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace DMP.BL.Services;

public class RedisService(IConnectionMultiplexer muxer) : IRedisService
{
    private readonly IDatabase _redis = muxer.GetDatabase();
    private readonly JsonCommands _json = muxer.GetDatabase().JSON();

    public async Task<T?> GetCacheValueAsync<T>(string key)
    {
        var cachedValue = await _redis.StringGetAsync(key);

        return cachedValue.HasValue
            ? JsonSerializer.Deserialize<T>(cachedValue.ToString(), CacheKeys.JsonSerializerOptions)
            : default;
    }

    public async Task SetProductAsync(Product product, string rootCategoryId)
    {
        var json = JsonSerializer.Serialize(product, CacheKeys.JsonSerializerOptions);
        await _json.SetAsync(string.Format(CacheKeys.ProductKey, rootCategoryId, product.ProductId), "$", json);
    }

    public async Task<string?> GetSysNotificationAsync()
    {
        // BRPOP with a 2-second timeout; the reply is [key, value] or nil.
        var result = await _redis.ExecuteAsync("BRPOP", CacheKeys.SysNotificationQueue, "2");
        if (result.IsNull)
        {
            return null;
        }

        var values = (RedisResult[])result!;
        return values[1].ToString();
    }
}
