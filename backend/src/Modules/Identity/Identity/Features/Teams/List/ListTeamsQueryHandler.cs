using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Domain.Teams;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Identity.Features.Teams.List;

public sealed class ListTeamsQueryHandler(IdentityDbContext dbContext) : IRequestHandler<ListTeamsQuery, Result<KeysetCursorPage<TeamView>>>
{
    public async ValueTask<Result<KeysetCursorPage<TeamView>>> Handle(ListTeamsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var teams = Filter(dbContext, query);
        var descending = SortDirections.IsDescending(query.Order);
        var page = (string.IsNullOrEmpty(query.Sort) ? TeamListRules.SortName : query.Sort) switch
        {
            TeamListRules.SortCreatedAt => await KeysetPages.ReadAsync(
                teams, cursor, TeamListRules.SortCreatedAt, team => (DateTime?)team.CreatedAt, team => team.Id, RowOf,
                row => (DateTime?)row.CreatedAt, row => row.Id, KeysetOrdering.ParseTimestamp, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            _ => await KeysetPages.ReadAsync(
                teams, cursor, TeamListRules.SortName, team => team.NameNormalized, team => team.Id, RowOf,
                row => row.NameNormalized, row => row.Id, KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
        };

        return page is null
            ? Failure()
            : Result<KeysetCursorPage<TeamView>>.Success(new KeysetCursorPage<TeamView>(page.Items.Select(ToView).ToArray(), page.NextCursor));
    }

    private static IQueryable<Team> Filter(IdentityDbContext dbContext, ListTeamsQuery query)
    {
        var teams = dbContext.Teams.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            teams = teams.Where(team => EF.Functions.ILike(team.NameNormalized, pattern));
        }

        return teams;
    }

    private static readonly Expression<Func<Team, TeamRow>> RowOf =
        team => new TeamRow(team.Id, team.Name, team.NameNormalized, team.CreatedAt);

    private static TeamView ToView(TeamRow row) => new(row.Id, row.Name);

    private static Result<KeysetCursorPage<TeamView>> Failure() =>
        Result<KeysetCursorPage<TeamView>>.Failure(PagingErrors.Field(nameof(ListTeamsQuery.After)));
}
