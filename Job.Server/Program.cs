using DMP.BL.Services;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Job.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.UseUtcTimestamp = true;
});

var dmpConnectionString = builder.Configuration.GetConnectionString("DmpConnection") is { Length: > 0 } connectionString
    ? connectionString
    : throw new InvalidOperationException("Connection string 'DmpConnection' is not configured.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseFilter(new DisableConcurrentExecutionFilter())
    .UsePostgreSqlStorage(
        options => options.UseNpgsqlConnection(dmpConnectionString),
        new PostgreSqlStorageOptions { DistributedLockTimeout = TimeSpan.FromMinutes(5) }));
builder.Services.AddHangfireServer();

builder.Services
    .AddDmpRedis(builder.Configuration)
    .AddDmpDbContexts(dmpConnectionString)
    .AddDmpServices(builder.Configuration);

var app = builder.Build();

var dashboardSettings = builder.Configuration
    .GetSection(HangfireDashboardSettings.SectionName)
    .Get<HangfireDashboardSettings>() ?? new HangfireDashboardSettings();

IDashboardAuthorizationFilter[] dashboardAuthorization = [];
if (dashboardSettings.IsAuthEnabled)
{
    dashboardAuthorization = [new HangfireBasicAuthFilter(dashboardSettings.Username!, dashboardSettings.Password!)];
}
else
{
    app.Logger.LogWarning(
        "Hangfire dashboard authentication is not configured ({Section}:Username/Password); the dashboard is open to anyone who can reach it",
        HangfireDashboardSettings.SectionName);
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = dashboardAuthorization });

// Recurring job IDs and schedules are referenced by existing Hangfire storage; keep them stable.
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
var recurringJobOptions = new RecurringJobOptions { MisfireHandling = MisfireHandlingMode.Ignorable };

recurringJobs.AddOrUpdate<IMailService>(
    "mail-sender-job", job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely(), recurringJobOptions);
recurringJobs.AddOrUpdate<IProductCacheService>(
    "product-cache-job", job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely(), recurringJobOptions);
recurringJobs.AddOrUpdate<INotificationsService>(
    "notifications-job", job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely(), recurringJobOptions);

app.Run();
