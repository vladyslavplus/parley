using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Identity.Contracts.Events;

/// <summary>
/// An active member of the organization was deactivated. Carries identifiers only. Consumers end whatever the member could
/// still reach through the organization, such as personal Telegram links.
/// </summary>
public sealed record MemberDeactivated : IntegrationEvent
{
    public Guid UserId { get; init; }
}
