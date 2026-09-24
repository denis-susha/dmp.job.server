using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace Job.Server;

/// <summary>
/// HTTP Basic authentication for the Hangfire dashboard.
/// </summary>
public sealed class HangfireBasicAuthFilter(string username, string password) : IDashboardAuthorizationFilter
{
    private readonly byte[] _expectedUsername = Encoding.UTF8.GetBytes(username);
    private readonly byte[] _expectedPassword = Encoding.UTF8.GetBytes(password);

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (IsAuthorized(httpContext.Request.Headers.Authorization.ToString()))
        {
            return true;
        }

        // Hangfire only responds with 401; the challenge header makes browsers prompt for credentials.
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        return false;
    }

    private bool IsAuthorized(string authorizationHeader)
    {
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var header)
            || !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
        {
            return false;
        }

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = credentials.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        var usernameMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(credentials[..separatorIndex]), _expectedUsername);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(credentials[(separatorIndex + 1)..]), _expectedPassword);

        return usernameMatches & passwordMatches;
    }
}
