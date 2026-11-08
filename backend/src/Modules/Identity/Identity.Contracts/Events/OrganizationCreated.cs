using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Identity.Contracts.Events;

/// <summary>
/// Published when an organization is created at registration. Carries identifiers
/// only; consumers read anything else through their own module contracts.
/// </summary>
public sealed record OrganizationCreated : IntegrationEvent
{
    public Guid OrganizationId { get; init; }
}
