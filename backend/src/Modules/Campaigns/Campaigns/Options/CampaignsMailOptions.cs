using MailKit.Security;

namespace Parley.Campaigns.Options;

/// <summary>
/// SMTP settings of the campaign sender. The campaigns module binds the same <c>Email</c> section as the notifications
/// sender, with its own type: modules share configuration keys, never types. Validated on start.
/// </summary>
public sealed class CampaignsMailOptions
{
    public const string SectionName = "Email";

    public const int MinimumPort = 1;

    public const int MaximumPort = 65535;

    /// <summary>SMTP server host. Environment data: configuration must set it.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port. Environment data: zero until configuration sets it.</summary>
    public int Port { get; set; }

    /// <summary>Sender address. Environment data: configuration must set it.</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public SecureSocketOptions SecureSocketOption { get; set; } = SecureSocketOptions.None;

    /// <summary>SMTP login. Empty when the server needs no authentication.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP password. Supplied through the environment in real deployments.</summary>
    public string Password { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
