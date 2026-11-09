using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>An open task reached its reminder window. Published once per task. Carries identifiers and the due instant only.</summary>
public sealed record TaskDueSoon : IntegrationEvent
{
    public Guid TaskId { get; init; }

    public Guid AssigneeUserId { get; init; }

    public DateTime DueAt { get; init; }
}
