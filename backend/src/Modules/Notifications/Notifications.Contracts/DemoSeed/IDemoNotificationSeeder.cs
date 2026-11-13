namespace Parley.Notifications.Contracts.DemoSeed;

/// <summary>Seeds a handful of in-app notifications into the demo users, so the bell is not empty on a fresh workspace.</summary>
public interface IDemoNotificationSeeder
{
    /// <summary>Writes the demo notifications of every given recipient. A re-run changes nothing.</summary>
    Task SeedAsync(
        Guid organizationId,
        IReadOnlyList<Guid> recipientUserIds,
        Guid actorUserId,
        DateTimeOffset reference,
        CancellationToken cancellationToken);

    /// <summary>Removes every notification and delivery of the organization, for a stale demo refresh.</summary>
    Task WipeAsync(Guid organizationId, CancellationToken cancellationToken);
}
