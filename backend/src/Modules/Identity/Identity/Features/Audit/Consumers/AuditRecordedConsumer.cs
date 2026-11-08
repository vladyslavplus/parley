using Parley.Identity.Domain.Audit;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Identity.Features.Audit.Consumers;

/// <summary>
/// Writes each <see cref="AuditRecorded"/> event as an audit row of the Identity schema.
/// The dispatcher runs this in the same transaction as the inbox row, so a replayed event
/// is a no-op. The consumer only changes the tracked graph; the dispatcher saves it.
/// </summary>
public sealed class AuditRecordedConsumer(IdentityDbContext dbContext) : IIntegrationEventConsumer<AuditRecorded>
{
    public Task ConsumeAsync(AuditRecorded integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.AuditEvents.Add(new AuditEvent(integrationEvent.OrganizationId)
        {
            ActorUserId = integrationEvent.ActorUserId,
            Action = integrationEvent.Action,
            TargetType = integrationEvent.EntityType,
            TargetId = integrationEvent.EntityId,
            Metadata = AuditMetadata.Empty,
            OccurredAt = integrationEvent.OccurredAt.UtcDateTime,
            SourceEventId = integrationEvent.EventId,
        });
        return Task.CompletedTask;
    }
}
