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

    /// <summary>Domain of generated message ids in tests.</summary>
    public const string MessageIdDomain = "parley.test";

    /// <summary>Base address of the API in unsubscribe links of tests.</summary>
    public const string PublicApiBaseUrlForLinks = "http://api.parley.test";

    /// <summary>Queue no test host polls. Campaign send jobs are run by the tests, never by a worker.</summary>
    public const string UnpolledCampaignSendQueue = "campaigns-send-unpolled";

    /// <summary>Address of the scoring service for hosts that never call it; the committed configuration has a placeholder.</summary>
    public const string ScoringBaseUrl = "http://127.0.0.1:8090/";

    /// <summary>Send rate of tests: effectively unlimited, so a send costs no measurable wait.</summary>
    public const string TestRatePerSecond = "1000";

    /// <summary>Retry backoff of tests: none, so a retry is due at once and a test does not wait for it.</summary>
    public const string TestRetryBackoff = "00:00:00";

    /// <summary>Random per test run; it signs unsubscribe tokens inside the test process only.</summary>
    public static readonly string UnsubscribeTokenKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

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
        ["Insights:Scoring:BaseUrl"] = ScoringBaseUrl,
        // A local Development file may hold real keys; the tests must never call Telegram or the scoring service unless a
        // test supplies its own values. The demo seed stays off; its own test builds the runner with explicit options.
        ["Insights:Scoring:ApiKey"] = string.Empty,
        ["Telegram:BotToken"] = string.Empty,
        ["Telegram:BotUsername"] = string.Empty,
        ["Telegram:WebhookSecret"] = string.Empty,
        ["Demo:Seed:Enabled"] = "false",
        ["Demo:Seed:AccountPassword"] = string.Empty,
        ["Campaigns:Send:QueueName"] = UnpolledCampaignSendQueue,
        ["Campaigns:Send:MessageIdDomain"] = MessageIdDomain,
        ["Campaigns:Send:UnsubscribeTokenKey"] = UnsubscribeTokenKey,
        ["Campaigns:Send:PublicApiBaseUrl"] = PublicApiBaseUrlForLinks,
        ["Campaigns:Send:PublicWebBaseUrl"] = PublicBaseUrl,
        ["Campaigns:Send:RatePerSecond"] = TestRatePerSecond,
        ["Campaigns:Send:RetryBackoff"] = TestRetryBackoff,
    };
}
