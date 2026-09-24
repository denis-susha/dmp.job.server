using DMP.BL.Models.AppSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMP.BL.Services;

/// <summary>
/// Drains the Redis system-notification queue and forwards each message to a Telegram chat.
/// </summary>
public class NotificationsService(
    ILogger<NotificationsService> logger,
    IRedisService redisService,
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramSettings> telegramSettings) : INotificationsService
{
    public const string TelegramHttpClientName = "Telegram";

    private readonly TelegramSettings _telegramSettings = telegramSettings.Value;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (await redisService.GetSysNotificationAsync() is { } message)
        {
            if (_telegramSettings.Enabled)
            {
                await SendToTelegramAsync(message, cancellationToken);
            }
            else
            {
                logger.LogInformation("Telegram messages are disabled. Message: {Message}", message);
            }
        }
    }

    private async Task SendToTelegramAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(TelegramHttpClientName);

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = _telegramSettings.SysChatId,
                ["text"] = message,
                ["parse_mode"] = "HTML",
            });

            using var response = await client.PostAsync($"bot{_telegramSettings.BotToken}/sendMessage", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // The message is dropped after logging; the loop continues with the next notification.
            logger.LogError(ex, "Failed to send system notification to Telegram");
        }
    }
}
