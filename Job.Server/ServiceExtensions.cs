using DMP.BL.Models.AppSettings;
using DMP.BL.Services;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RazorLight;
using StackExchange.Redis;

namespace Job.Server;

public static class ServiceExtensions
{
    private static readonly Uri _telegramApiBaseAddress = new("https://api.telegram.org/");

    public static IServiceCollection AddDmpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<TelegramSettings>(configuration.GetSection("TelegramSettings"));

        services.AddHttpClient(
            NotificationsService.TelegramHttpClientName,
            client => client.BaseAddress = _telegramApiBaseAddress);

        services.AddTransient<IRedisService, RedisService>();
        services.AddTransient<IMailService, MailService>();
        services.AddTransient<IProductCacheService, ProductCacheService>();
        services.AddTransient<INotificationsService, NotificationsService>();

        services.AddSingleton<IRazorLightEngine>(_ => new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .Build());

        return services;
    }

    public static IServiceCollection AddDmpDbContexts(this IServiceCollection services, string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        // Required to map POCOs and dictionaries to jsonb columns.
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContextFactory<DmpDbContext>(options => options.UseNpgsql(dataSource));

        return services;
    }

    /// <summary>
    /// Registers the Redis connection configured via the <c>REDIS_HOST</c>, <c>REDIS_PORT</c> and
    /// <c>REDIS_PASSWORD</c> environment variables (shared with the other DMP services).
    /// </summary>
    public static IServiceCollection AddDmpRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString =
            $"{configuration["REDIS_HOST"]}:{configuration["REDIS_PORT"]},password={configuration["REDIS_PASSWORD"]},abortConnect=False";

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        return services;
    }
}
