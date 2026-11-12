using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Insights.Contracts.Events;

/// <summary>The ML settings of an organization changed. Carries identifiers and the new values only.</summary>
public sealed record MlSettingsChanged : IntegrationEvent
{
    public Guid OrganizationId { get; init; }

    public bool MlEnabled { get; init; }

    /// <summary>The stored uplift model choice after the change; null means the newest synced uplift model.</summary>
    public string? ActiveUpliftVersion { get; init; }

    public Guid ChangedByUserId { get; init; }
}
