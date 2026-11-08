using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Audit.List;

public sealed class ListAuditEventsQueryHandler(IdentityDbContext dbContext) : IRequestHandler<ListAuditEventsQuery, Result<CursorPage<AuditEventView>>>
{
    public async ValueTask<Result<CursorPage<AuditEventView>>> Handle(ListAuditEventsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var before = query.Before;

        var rows = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => before == null || auditEvent.Id < before.Value)
            .OrderByDescending(auditEvent => auditEvent.Id)
            .Select(auditEvent => new AuditEventView(
                auditEvent.Id,
                auditEvent.ActorUserId,
                auditEvent.Action,
                auditEvent.TargetType,
                auditEvent.TargetId,
                auditEvent.OccurredAt))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        return Result<CursorPage<AuditEventView>>.Success(KeysetPage.From(rows, limit, view => view.Id));
    }
}
