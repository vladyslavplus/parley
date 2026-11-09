using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A deal was won or lost. Carries identifiers and the final status only.</summary>
public sealed record DealClosed : IntegrationEvent
{
    public Guid DealId { get; init; }

    public Guid OwnerUserId { get; init; }

    /// <summary>Final status: <c>won</c> or <c>lost</c>.</summary>
    public string Status { get; init; } = string.Empty;
}
