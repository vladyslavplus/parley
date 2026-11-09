using Parley.Crm.Shared.Pipelines;
using Parley.Identity.Contracts.Events;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Crm.Features.Pipelines.Consumers;

/// <summary>
/// Adds the default pipeline of a new organization, named in the organization's locale. The dispatcher runs this in
/// the inbox transaction, so a replayed event is a no-op; the provisioner also skips an organization that already has
/// a default pipeline.
/// </summary>
public sealed class OrganizationCreatedConsumer(DefaultPipelineProvisioner provisioner)
    : IIntegrationEventConsumer<OrganizationCreated>
{
    public async Task ConsumeAsync(OrganizationCreated integrationEvent, CancellationToken cancellationToken)
    {
        await provisioner.AddIfMissingAsync(
            integrationEvent.OrganizationId,
            OrganizationLocales.OrDefault(integrationEvent.DefaultLocale),
            cancellationToken);
    }
}
