using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Domain.Audit;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Identity.Features.Audit.List;

public sealed class ListAuditEventsQueryHandler(IdentityDbContext dbContext) : IRequestHandler<ListAuditEventsQuery, Result<KeysetCursorPage<AuditEventView>>>
{
    public async ValueTask<Result<KeysetCursorPage<AuditEventView>>> Handle(ListAuditEventsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var page = await KeysetPages.ReadAsync(
            Filter(dbContext, query),
            cursor,
            AuditListRules.SortOccurredAt,
            auditEvent => (DateTime?)auditEvent.OccurredAt,
            auditEvent => auditEvent.Id,
            RowOf,
            view => (DateTime?)view.OccurredAt,
            view => view.Id,
            KeysetOrdering.ParseTimestamp,
            KeysetOrdering.FormatTimestamp,
            SortDirections.IsDescending(query.Order, defaultDescending: true),
            limit,
            cancellationToken);

        return page is null
            ? Failure()
            : Result<KeysetCursorPage<AuditEventView>>.Success(page);
    }

    private static IQueryable<AuditEvent> Filter(IdentityDbContext dbContext, ListAuditEventsQuery query)
    {
        var events = dbContext.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action;
            events = events.Where(auditEvent => auditEvent.Action == action);
        }

        if (!string.IsNullOrEmpty(query.ActorUserId))
        {
            var actorUserId = Guid.Parse(query.ActorUserId);
            events = events.Where(auditEvent => auditEvent.ActorUserId == actorUserId);
        }

        var from = AuditListRules.ParseInstant(query.From);
        if (from is not null)
        {
            var fromInstant = from.Value;
            events = events.Where(auditEvent => auditEvent.OccurredAt >= fromInstant);
        }

        var to = AuditListRules.ParseInstant(query.To);
        if (to is not null)
        {
            var toInstant = to.Value;
            events = events.Where(auditEvent => auditEvent.OccurredAt < toInstant);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            events = events.Where(auditEvent =>
                EF.Functions.ILike(auditEvent.Action, pattern)
                || (auditEvent.TargetType != null && EF.Functions.ILike(auditEvent.TargetType, pattern)));
        }

        return events;
    }

    private static readonly Expression<Func<AuditEvent, AuditEventView>> RowOf = auditEvent => new AuditEventView(
        auditEvent.Id,
        auditEvent.ActorUserId,
        auditEvent.Action,
        auditEvent.TargetType,
        auditEvent.TargetId,
        auditEvent.OccurredAt);

    private static Result<KeysetCursorPage<AuditEventView>> Failure() =>
        Result<KeysetCursorPage<AuditEventView>>.Failure(PagingErrors.Field(nameof(ListAuditEventsQuery.After)));
}
