using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Identity.Contracts.Events;

/// <summary>
/// A user became an active member of the organization through an accepted invitation, new or reactivated. Carries
/// identifiers only. The inviter is named so a consumer can tell them.
/// </summary>
public sealed record MemberActivated : IntegrationEvent
{
    public Guid UserId { get; init; }

    public Guid InvitedByUserId { get; init; }
}
