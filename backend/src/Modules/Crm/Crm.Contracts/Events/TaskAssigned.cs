using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>
/// A task was created or its assignee changed. Carries identifiers only. The actor is the user who made the change, so
/// a consumer can skip a self-assignment.
/// </summary>
public sealed record TaskAssigned : IntegrationEvent
{
    public Guid TaskId { get; init; }

    public Guid AssigneeUserId { get; init; }

    public Guid ActorUserId { get; init; }
}
