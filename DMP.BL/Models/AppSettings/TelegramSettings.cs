namespace DMP.BL.Models.AppSettings;

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string SysChatId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
