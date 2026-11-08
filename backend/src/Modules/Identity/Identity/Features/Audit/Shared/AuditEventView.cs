namespace Parley.Identity.Features.Audit.Shared;

/// <summary>
/// One audit row. It holds identifiers and a constant action code only; it never holds
/// names, addresses or field values.
/// </summary>
public sealed record AuditEventView(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string? TargetType,
    Guid? TargetId,
    DateTime OccurredAt);
