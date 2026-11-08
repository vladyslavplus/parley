using MailKit.Security;
using Parley.SharedKernel.Localization;

namespace Parley.Notifications.Options;

/// <summary>
/// SMTP settings for transactional email. Bind to the <c>Email</c> section;
/// validated on start. Overridable by environment variables such as
/// <c>Email__Host</c> and <c>Email__Password</c> so no real credentials are
/// committed. <see cref="DefaultCulture"/> is the fallback for unknown
/// cultures requested by callers; valid values are <c>uk</c> and <c>en</c>.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public const int MinimumPort = 1;

    public const int MaximumPort = 65535;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan MinimumTimeout = TimeSpan.FromSeconds(1);

    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromMinutes(5);

    /// <summary>SMTP server host. Environment data: no code default, configuration must set it.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port. Environment data: zero until configuration sets it, which fails validation.</summary>
    public int Port { get; set; }

    /// <summary>Sender address. Environment data: no code default, configuration must set it.</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = EmailDefaults.FromName;

    public SecureSocketOptions SecureSocketOption { get; set; } = SecureSocketOptions.None;

    /// <summary>SMTP login. Empty when the server needs no authentication.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP password. Supplied through the environment in real deployments.</summary>
    public string Password { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; } = DefaultTimeout;

    /// <summary>Fallback culture for unknown cultures. Valid values are <see cref="CultureCodes.Ukrainian"/> and <see cref="CultureCodes.English"/>.</summary>
    public string DefaultCulture { get; set; } = CultureCodes.Default;

    public static bool IsSupportedDefaultCulture(string? value) =>
        string.Equals(value, CultureCodes.Ukrainian, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, CultureCodes.English, StringComparison.OrdinalIgnoreCase);
}
