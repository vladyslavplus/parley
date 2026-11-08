using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Audit.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Audit.List;

/// <summary>
/// Lists the audit events of the current organization, newest first. Keyset-paged by
/// identifier, which is time-ordered.
/// </summary>
/// <param name="Before">Identifier of the last row of the previous page, or null for the newest page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
[RequiresPermission(Permissions.AuditRead)]
public sealed record ListAuditEventsQuery(Guid? Before, int? Limit) : IRequest<Result<CursorPage<AuditEventView>>>;
