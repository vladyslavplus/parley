using Parley.SharedKernel.Outbox.Publishing;

namespace Parley.SharedKernel.Outbox.Audit;

/// <summary>
/// Publishes <see cref="AuditRecorded"/> through <see cref="IOutbox"/> so the
/// audit event commits in the same transaction as the change it describes.
/// </summary>
public static class AuditRecordedOutboxExtensions
{
    public static IOutbox AddAuditRecorded(
        this IOutbox outbox,
        Guid? actorUserId,
        Guid organizationId,
        string action,
        TimeProvider timeProvider,
        string? entityType = null,
        Guid? entityId = null)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var recorded = new AuditRecorded
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = timeProvider.GetUtcNow(),
            ActorUserId = actorUserId,
            OrganizationId = organizationId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
        };
        outbox.Add(recorded);
        return outbox;
    }
}
