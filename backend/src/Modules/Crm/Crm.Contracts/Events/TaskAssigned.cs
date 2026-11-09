using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A task was created or its assignee changed. Carries identifiers only.</summary>
public sealed record TaskAssigned : IntegrationEvent
{
    public Guid TaskId { get; init; }

    public Guid AssigneeUserId { get; init; }
}
