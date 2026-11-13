namespace Parley.Identity.Domain.DemoSeed;

/// <summary>
/// The instant the demo workspace of one organization was last seeded completely. It is a platform marker of the demo
/// feature, not a business row: the seed uses it to skip a fresh workspace and to regenerate a stale one.
/// </summary>
public sealed class DemoSeedState
{
    public Guid OrganizationId { get; set; }

    public DateTime SeededAt { get; set; }
}
