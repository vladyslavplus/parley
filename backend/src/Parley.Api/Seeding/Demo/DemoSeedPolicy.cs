
namespace Parley.Api.Seeding.Demo;

/// <summary>Pure decisions of the demo seed: whether the host may run it and whether the stored workspace is fresh.</summary>
public static class DemoSeedPolicy
{
    /// <summary>Only a Development host may generate demo data, whatever the switch says.</summary>
    public static bool MayRun(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.IsDevelopment();
    }

    /// <summary>True while the last complete seed is younger than the refresh window.</summary>
    public static bool IsFresh(DateTime? lastSeededAtUtc, DateTime nowUtc, int refreshAfterDays) =>
        lastSeededAtUtc is { } last && nowUtc - last < TimeSpan.FromDays(refreshAfterDays);
}
