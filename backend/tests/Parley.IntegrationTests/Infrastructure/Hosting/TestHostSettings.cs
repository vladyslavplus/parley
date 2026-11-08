using System.Globalization;
using System.Security.Cryptography;

namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Settings every integration test host needs to pass startup validation. The
/// committed configuration holds placeholders and the Development file is
/// git-ignored, so the tests supply these values themselves and never read either file.
/// </summary>
public static class TestHostSettings
{
    /// <summary>Random per test run; it signs tokens inside the test process only.</summary>
    public static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public const string PublicBaseUrl = "http://localhost:3000";

    public const string OtlpEndpoint = "http://127.0.0.1:4317";

    public const string FromAddress = "no-reply@parley.test";

    public const string FromName = "Parley";

    /// <summary>SMTP address for hosts that never send mail. Nothing listens there.</summary>
    public const string UnusedSmtpHost = "127.0.0.1";

    public const int UnusedSmtpPort = 1;

    /// <summary>
    /// Settings for a test host except the database connection string, which each
    /// fixture supplies. Mail-sending hosts pass their Mailpit address.
    /// </summary>
    public static Dictionary<string, string?> StartupSettings(string smtpHost, int smtpPort) => new()
    {
        ["Telemetry:OtlpEndpoint"] = OtlpEndpoint,
        ["Auth:SigningKey"] = SigningKey,
        ["Auth:PublicBaseUrl"] = PublicBaseUrl,
        ["Email:Host"] = smtpHost,
        ["Email:Port"] = smtpPort.ToString(CultureInfo.InvariantCulture),
        ["Email:FromAddress"] = FromAddress,
        ["Email:FromName"] = FromName,
    };
}
