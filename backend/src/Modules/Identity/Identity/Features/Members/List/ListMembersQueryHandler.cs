using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Identity.Features.Members.List;

public sealed class ListMembersQueryHandler(IdentityDbContext dbContext)
    : IRequestHandler<ListMembersQuery, Result<KeysetCursorPage<MemberView>>>
{
    public async ValueTask<Result<KeysetCursorPage<MemberView>>> Handle(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var members = Filter(dbContext, query);
        var descending = SortDirections.IsDescending(query.Order);
        var page = (string.IsNullOrEmpty(query.Sort) ? MemberListRules.SortName : query.Sort) switch
        {
            MemberListRules.SortEmail => await KeysetPages.ReadAsync(
                members, cursor, MemberListRules.SortEmail, EmailKey(dbContext), membership => membership.Id, RowOf(dbContext),
                row => row.Email ?? string.Empty, row => row.Id, KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
            MemberListRules.SortRole => await KeysetPages.ReadAsync(
                members, cursor, MemberListRules.SortRole, RoleKey(dbContext), membership => membership.Id, RowOf(dbContext),
                row => row.RoleCode, row => row.Id, KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
            MemberListRules.SortJoinedAt => await KeysetPages.ReadAsync(
                members, cursor, MemberListRules.SortJoinedAt, membership => (DateTime?)membership.JoinedAt, membership => membership.Id, RowOf(dbContext),
                row => (DateTime?)row.JoinedAt, row => row.Id, KeysetOrdering.ParseTimestamp, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            _ => await KeysetPages.ReadAsync(
                members, cursor, MemberListRules.SortName, NameKey(dbContext), membership => membership.Id, RowOf(dbContext),
                row => row.DisplayName ?? row.Email ?? string.Empty, row => row.Id, KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
        };

        return page is null
            ? Failure()
            : Result<KeysetCursorPage<MemberView>>.Success(new KeysetCursorPage<MemberView>(page.Items.Select(ToView).ToArray(), page.NextCursor));
    }

    private static IQueryable<Membership> Filter(IdentityDbContext dbContext, ListMembersQuery query)
    {
        var members = dbContext.Memberships.AsNoTracking();
        var status = string.IsNullOrEmpty(query.Status) ? MemberListRules.StatusActive : query.Status;
        if (status != MemberListRules.StatusAll)
        {
            members = members.Where(membership => membership.Status == status);
        }

        if (!string.IsNullOrEmpty(query.RoleCode))
        {
            var roleCode = query.RoleCode;
            members = members.Where(membership => dbContext.Roles.Any(role => role.Id == membership.RoleId && role.Code == roleCode));
        }

        if (!string.IsNullOrEmpty(query.TeamId))
        {
            var teamId = Guid.Parse(query.TeamId);
            members = members.Where(membership => membership.TeamId == teamId);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            members = members.Where(membership => dbContext.Users.Any(user =>
                user.Id == membership.UserId
                && ((user.DisplayName != null && EF.Functions.ILike(user.DisplayName, pattern))
                    || (user.Email != null && EF.Functions.ILike(user.Email, pattern)))));
        }

        return members;
    }

    /// <summary>Sort key: the display name, or the email when the user has no name.</summary>
    private static Expression<Func<Membership, string>> NameKey(IdentityDbContext dbContext) =>
        membership => dbContext.Users
            .Where(user => user.Id == membership.UserId)
            .Select(user => user.DisplayName ?? user.Email ?? string.Empty)
            .FirstOrDefault() ?? string.Empty;

    private static Expression<Func<Membership, string>> EmailKey(IdentityDbContext dbContext) =>
        membership => dbContext.Users
            .Where(user => user.Id == membership.UserId)
            .Select(user => user.Email ?? string.Empty)
            .FirstOrDefault() ?? string.Empty;

    private static Expression<Func<Membership, string>> RoleKey(IdentityDbContext dbContext) =>
        membership => dbContext.Roles
            .Where(role => role.Id == membership.RoleId)
            .Select(role => role.Code)
            .FirstOrDefault() ?? string.Empty;

    /// <summary>The columns of one member row, read in the same statement as the page.</summary>
    private static Expression<Func<Membership, MemberRow>> RowOf(IdentityDbContext dbContext) =>
        membership => new MemberRow(
            membership.Id,
            membership.UserId,
            dbContext.Users.Where(user => user.Id == membership.UserId).Select(user => user.Email).FirstOrDefault(),
            dbContext.Users.Where(user => user.Id == membership.UserId).Select(user => user.DisplayName).FirstOrDefault(),
            dbContext.Roles.Where(role => role.Id == membership.RoleId).Select(role => role.Code).FirstOrDefault() ?? string.Empty,
            membership.TeamId,
            membership.Status,
            membership.JoinedAt);

    private static MemberView ToView(MemberRow row) =>
        new(row.Id, row.UserId, row.Email, row.DisplayName, row.RoleCode, row.TeamId, row.Status, row.JoinedAt);

    private static Result<KeysetCursorPage<MemberView>> Failure() =>
        Result<KeysetCursorPage<MemberView>>.Failure(PagingErrors.Field(nameof(ListMembersQuery.After)));
}
