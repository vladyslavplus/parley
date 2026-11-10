using Parley.Campaigns.Contracts.Events;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Activities.Consumers;

/// <summary>Writes the timeline entry of a contact that unsubscribed from campaign email. Idempotent by source event id.</summary>
public sealed class RecipientUnsubscribedConsumer(CrmDbContext dbContext, ITenantContext tenantContext)
    : IIntegrationEventConsumer<RecipientUnsubscribed>
{
    public Task ConsumeAsync(RecipientUnsubscribed integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.Activities.Add(new Activity
        {
            OrganizationId = tenantContext.OrganizationId,
            Type = ActivityTypes.Unsubscribed,
            ContactId = integrationEvent.ContactId,
            Payload = CampaignActivityPayload.For(integrationEvent.CampaignId),
            OccurredAt = integrationEvent.SuppressedAt.UtcDateTime,
            SourceEventId = integrationEvent.EventId,
        });
        return Task.CompletedTask;
    }
}
