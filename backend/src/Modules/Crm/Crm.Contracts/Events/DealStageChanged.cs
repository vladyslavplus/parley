using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A deal moved to another stage. <see cref="FromStageId"/> is null for a stage it was never on before. Carries identifiers and codes only.</summary>
public sealed record DealStageChanged : IntegrationEvent
{
    public Guid DealId { get; init; }

    public Guid? FromStageId { get; init; }

    public Guid ToStageId { get; init; }

    /// <summary>Status of the deal after the move: <c>open</c>, <c>won</c> or <c>lost</c>.</summary>
    public string Status { get; init; } = string.Empty;
}
