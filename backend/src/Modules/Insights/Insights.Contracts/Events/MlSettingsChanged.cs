using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Insights.Contracts.Events;

/// <summary>The ML switch of an organization changed. Carries identifiers and the new switch value only.</summary>
public sealed record MlSettingsChanged : IntegrationEvent
{
    public Guid OrganizationId { get; init; }

    public bool MlEnabled { get; init; }

    public Guid ChangedByUserId { get; init; }
}
