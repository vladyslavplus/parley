using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Events;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Insights.Features.MlSettings.Consumers;

/// <summary>
/// Creates the ML settings row of a new organization with ML switched off. The read path also treats a missing row as off,
/// so an organization created before this consumer existed needs no backfill. The inbox runs this in its transaction, and
/// a replayed event finds the row and does nothing.
/// </summary>
public sealed class OrganizationCreatedConsumer(InsightsDbContext dbContext) : IIntegrationEventConsumer<OrganizationCreated>
{
    public async Task ConsumeAsync(OrganizationCreated integrationEvent, CancellationToken cancellationToken)
    {
        var exists = await dbContext.MlSettings.IgnoreQueryFilters()
            .AnyAsync(setting => setting.OrganizationId == integrationEvent.OrganizationId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.MlSettings.Add(new MlSetting { OrganizationId = integrationEvent.OrganizationId, MlEnabled = false });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
