namespace Parley.Api.Cli.SeedDemo;

/// <summary>Outcome of the target check.</summary>
/// <param name="Allowed">True when the seed may run.</param>
/// <param name="Reason">Why the seed is refused; null when allowed.</param>
public sealed record SeedDemoTargetDecision(bool Allowed, string? Reason)
{
    public static SeedDemoTargetDecision Allow { get; } = new(true, null);

    public static SeedDemoTargetDecision Refuse(string reason) => new(false, reason);
}
