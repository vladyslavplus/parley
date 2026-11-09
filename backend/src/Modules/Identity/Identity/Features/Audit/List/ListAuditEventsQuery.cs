using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Audit.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Audit.List;

/// <summary>
/// Lists the audit events of the current organization, newest first, keyset-paged.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the action code or the target type must contain.</param>
/// <param name="Action">Only events with this exact action code.</param>
/// <param name="ActorUserId">Only events of this actor, as GUID text.</param>
/// <param name="From">First instant included, UTC (inclusive).</param>
/// <param name="To">Instant excluded, UTC (exclusive).</param>
/// <param name="Sort">Sort field: only <c>occurredAt</c>.</param>
/// <param name="Order">Sort direction: <c>desc</c> (default) or <c>asc</c>.</param>
[RequiresPermission(Permissions.AuditRead)]
public sealed record ListAuditEventsQuery(
    string? After,
    int? Limit,
    string? Q,
    string? Action,
    string? ActorUserId,
    string? From,
    string? To,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<AuditEventView>>>;
