using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Identity.Contracts.Events;

/// <summary>
/// Published when an organization is created at registration. Carries identifiers
/// only; consumers read anything else through their own module contracts.
/// </summary>
public sealed record OrganizationCreated : IntegrationEvent
{
    public Guid OrganizationId { get; init; }

    /// <summary>Default locale of the organization, used to name rows seeded for it (for example pipeline stages).</summary>
    /// <summary>
    /// Default locale of the organization, used to name rows seeded for it (for example pipeline stages). Events
    /// written before the locale was carried are read with it missing; consumers then use the platform default.
    /// </summary>
    public string? DefaultLocale { get; init; }
}
