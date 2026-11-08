using Parley.SharedKernel.Outbox.Messages;

namespace Parley.SharedKernel.Outbox.Audit;

/// <summary>
/// The audit integration event envelope. Fields are identifiers and constant
/// codes only: the acting user, the organization, the action code, the
/// affected entity type and id, and the occurred-at instant. It carries NO
/// personal payload (no names, emails, messages or field values), so it can
/// cross module boundaries and be stored without leaking personal data.
/// The envelope format is the flat <see cref="IntegrationEvent"/> JSON
/// object documented on that type.
/// </summary>
public sealed record AuditRecorded : IntegrationEvent
{
    public Guid? ActorUserId { get; init; }

    public Guid OrganizationId { get; init; }

    /// <summary>
    /// A constant action code such as <c>crm.contact.created</c>. Callers
    /// pass the module's constant code, never a free-text description.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    public string? EntityType { get; init; }

    public Guid? EntityId { get; init; }
}
