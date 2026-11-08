using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Domain.Invitations;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Identity.Features.Invitations.List;

public sealed class ListInvitationsQueryHandler(IdentityDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ListInvitationsQuery, Result<KeysetCursorPage<InvitationView>>>
{
    public async ValueTask<Result<KeysetCursorPage<InvitationView>>> Handle(ListInvitationsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var invitations = Filter(dbContext, query, timeProvider.GetUtcNow().UtcDateTime);
        var sort = string.IsNullOrEmpty(query.Sort) ? InvitationListRules.SortCreatedAt : query.Sort;
        var descending = SortDirections.IsDescending(query.Order, defaultDescending: sort == InvitationListRules.SortCreatedAt);
        var page = sort switch
        {
            InvitationListRules.SortEmail => await KeysetPages.ReadAsync(
                invitations, cursor, InvitationListRules.SortEmail, invitation => invitation.EmailNormalized, invitation => invitation.Id,
                RowOf(dbContext), row => row.Email, row => row.Id, KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
            InvitationListRules.SortExpiresAt => await KeysetPages.ReadAsync(
                invitations, cursor, InvitationListRules.SortExpiresAt, invitation => (DateTime?)invitation.ExpiresAt, invitation => invitation.Id,
                RowOf(dbContext), row => (DateTime?)row.ExpiresAt, row => row.Id, KeysetOrdering.ParseTimestamp, KeysetOrdering.FormatTimestamp,
                descending, limit, cancellationToken),
            _ => await KeysetPages.ReadAsync(
                invitations, cursor, InvitationListRules.SortCreatedAt, invitation => (DateTime?)invitation.CreatedAt, invitation => invitation.Id,
                RowOf(dbContext), row => (DateTime?)row.CreatedAt, row => row.Id, KeysetOrdering.ParseTimestamp, KeysetOrdering.FormatTimestamp,
                descending, limit, cancellationToken),
        };

        return page is null
            ? Failure()
            : Result<KeysetCursorPage<InvitationView>>.Success(new KeysetCursorPage<InvitationView>(page.Items.Select(ToView).ToArray(), page.NextCursor));
    }

    /// <summary>Pending invitations: not accepted, not revoked and not expired, narrowed by the optional filters.</summary>
    private static IQueryable<Invitation> Filter(IdentityDbContext dbContext, ListInvitationsQuery query, DateTime nowUtc)
    {
        var invitations = dbContext.Invitations.AsNoTracking()
            .Where(invitation => invitation.AcceptedAt == null && invitation.RevokedAt == null && invitation.ExpiresAt > nowUtc);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            invitations = invitations.Where(invitation => EF.Functions.ILike(invitation.EmailNormalized, pattern));
        }

        if (!string.IsNullOrEmpty(query.RoleCode))
        {
            var roleCode = query.RoleCode;
            invitations = invitations.Where(invitation => dbContext.Roles.Any(role => role.Id == invitation.RoleId && role.Code == roleCode));
        }

        return invitations;
    }

    private static Expression<Func<Invitation, InvitationRow>> RowOf(IdentityDbContext dbContext) =>
        invitation => new InvitationRow(
            invitation.Id,
            invitation.EmailNormalized,
            dbContext.Roles.Where(role => role.Id == invitation.RoleId).Select(role => role.Code).FirstOrDefault() ?? string.Empty,
            invitation.TeamId,
            invitation.ExpiresAt,
            invitation.CreatedAt);

    private static InvitationView ToView(InvitationRow row) =>
        new(row.Id, row.Email, row.RoleCode, row.TeamId, row.ExpiresAt);

    private static Result<KeysetCursorPage<InvitationView>> Failure() =>
        Result<KeysetCursorPage<InvitationView>>.Failure(PagingErrors.Field(nameof(ListInvitationsQuery.After)));
}
