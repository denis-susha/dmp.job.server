namespace Job.Server;

public class HangfireDashboardSettings
{
    public const string SectionName = "HangfireDashboard";

    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool IsAuthEnabled => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
}
