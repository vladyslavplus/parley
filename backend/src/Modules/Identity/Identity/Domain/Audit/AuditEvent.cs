using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Identity.Domain.Audit;

/// <summary>
/// Append-only record of a security or business-critical action. Holds identifiers
/// and constant codes only, never personal payloads or secrets.
/// </summary>
public sealed class AuditEvent : BusinessEntity
{
    public AuditEvent()
    {
    }

    public AuditEvent(Guid organizationId) : base(organizationId)
    {
    }

    public Guid? ActorUserId { get; set; }

    /// <summary>A constant action code such as <c>identity.member.role_changed</c>.</summary>
    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    /// <summary>Safe metadata as a JSON object; never personal data.</summary>
    public string Metadata { get; set; } = "{}";

    public DateTime OccurredAt { get; set; }

    /// <summary>Identifier of the integration event that produced the row; unique so replays are no-ops.</summary>
    public Guid? SourceEventId { get; set; }
}
