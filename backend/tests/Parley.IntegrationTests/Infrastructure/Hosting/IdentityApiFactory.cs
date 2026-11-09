using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Parley.IntegrationTests.Api.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Outbox.Support;

namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// API host for authentication tests. Uses the shared Postgres container, a Mailpit
/// container for mail, a fake clock for refresh-window tests, and a Hangfire queue
/// that only this host polls, so jobs from other test hosts never run here.
/// </summary>
public sealed class IdentityApiFactory(
    PostgresFixture postgres,
    MailpitFixture mailpit,
    IReadOnlyDictionary<string, string?>? extraConfiguration = null,
    IPAddress? peerAddress = null,
    Action<IServiceCollection>? extraServices = null) : WebApplicationFactory<Program>
{
    private const string TestQueuePrefix = "identity-test-";

    private const string FastQueuePollInterval = "00:00:00.200";

    private const string RateLimitingSection = "RateLimiting";

    private const string PermitLimitKey = "PermitLimit";

    private const string WindowKey = "Window";

    private const string DefaultPermitLimit = "1000";

    private const string DefaultWindow = "01:00:00";

    private static readonly string[] RateLimitPolicyNames =
    [
        "Register",
        "Login",
        "Refresh",
        "ConfirmEmail",
        "ResendConfirmation",
        "ForgotPassword",
        "ResetPassword",
        "InvitationCreate",
        "InvitationAccept",
    ];

    private readonly string _queue = TestQueuePrefix + Guid.CreateVersion7().ToString("N");

    /// <summary>Contact export files of this host; removed when the host is disposed.</summary>
    private readonly string _exportDirectory = Path.Combine(Path.GetTempPath(), "parley-crm-exports-" + Guid.CreateVersion7().ToString("N"));

    /// <summary>Clock shared by the host. Tests advance it to cross the refresh grace window.</summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    /// <summary>Log entries written by this host, for leak checks.</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_exportDirectory))
        {
            Directory.Delete(_exportDirectory, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Generous limits by default so one test class does not exhaust a shared
        // per-address budget; the rate-limiting tests lower the policy they check.
        var configuration = new Dictionary<string, string?>();
        foreach (var policy in RateLimitPolicyNames)
        {
            configuration[$"{RateLimitingSection}:{policy}:{PermitLimitKey}"] = DefaultPermitLimit;
            configuration[$"{RateLimitingSection}:{policy}:{WindowKey}"] = DefaultWindow;
        }

        var settings = TestHostSettings.StartupSettings(mailpit.Host, mailpit.SmtpPort);
        settings["Database:ConnectionString"] = postgres.ConnectionString;
        settings["Hangfire:Queues:0"] = _queue;
        settings["Hangfire:QueuePollInterval"] = FastQueuePollInterval;
        settings["Auth:EmailQueueName"] = _queue;
        settings["CrmExports:QueueName"] = _queue;
        settings["CrmExports:Directory"] = _exportDirectory;
        foreach (var (key, value) in settings)
        {
            configuration[key] = value;
        }

        if (extraConfiguration is not null)
        {
            foreach (var (key, value) in extraConfiguration)
            {
                configuration[key] = value;
            }
        }

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services =>
        {
            // The API registers TimeProvider.System; the test clock must win.
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            if (peerAddress is not null)
            {
                // The test server reports no socket address; tests that need one set it here.
                services.AddSingleton<IStartupFilter>(new PeerAddressStartupFilter(peerAddress));
            }
            services.AddScoped<Parley.Identity.Authorization.Behavior.PermissionAuthorizationBehavior<Parley.IntegrationTests.Access.Support.PermissionProbeRequest, Parley.SharedKernel.Results.Result>>();
            // Outbox passes are driven explicitly by the tests that assert on them.
            OutboxDelivery.RemoveBackgroundLoops(services);
            services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly);
            extraServices?.Invoke(services);
        });
    }
}
