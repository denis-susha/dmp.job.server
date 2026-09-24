using System.Text.Json;

namespace DMP.BL.Constants;

/// <summary>
/// Redis keys shared with the web API. Values are part of the cross-service contract and must not change.
/// </summary>
internal static class CacheKeys
{
    /// <summary>Flat list of all menu categories (DAL shape), written by the web API.</summary>
    public const string DALAllMenuCategoriesKey = "dal:catalog:menu:categories";

    /// <summary>RedisJSON product document: 0 - root category id, 1 - product id.</summary>
    public const string ProductKey = "product:{0}:{1}";

    /// <summary>List used as a queue of system notifications forwarded to Telegram.</summary>
    public const string SysNotificationQueue = "sys_notify_queue";

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
