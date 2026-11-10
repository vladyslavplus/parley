using Parley.Campaigns.Contracts.Events;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Activities.Consumers;

/// <summary>
/// Writes the timeline entry of a contact that received a campaign email. The source event id is unique per
/// organization, so a replayed event adds nothing; the dispatcher's inbox guards the same thing one level up.
/// </summary>
public sealed class CampaignEmailSentConsumer(CrmDbContext dbContext, ITenantContext tenantContext)
    : IIntegrationEventConsumer<CampaignEmailSent>
{
    public Task ConsumeAsync(CampaignEmailSent integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.Activities.Add(new Activity
        {
            OrganizationId = tenantContext.OrganizationId,
            Type = ActivityTypes.CampaignEmailSent,
            ContactId = integrationEvent.ContactId,
            Payload = CampaignActivityPayload.For(integrationEvent.CampaignId),
            OccurredAt = integrationEvent.SentAt.UtcDateTime,
            SourceEventId = integrationEvent.EventId,
        });
        return Task.CompletedTask;
    }
}
