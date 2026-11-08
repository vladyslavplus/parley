namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Queue each test host's Hangfire server listens on. Every host sharing the
/// test database polls only its own queue, so job execution in a test is
/// deterministic and hosts never consume another test's jobs (which would
/// fail their activation without the probe registrations).
/// </summary>
public static class TestHangfireQueues
{
    public const string FixtureHost = "fixture";

    public const string IdempotencyHost = "idempotency";

    public const string HangfireHost = "default";
}
