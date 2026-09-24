namespace DMP.BL.Models.AppSettings;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool Ssl { get; set; }
}
