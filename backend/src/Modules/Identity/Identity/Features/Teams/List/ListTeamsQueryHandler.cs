using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.List;

public sealed class ListTeamsQueryHandler(IdentityDbContext dbContext) : IRequestHandler<ListTeamsQuery, Result<CursorPage<TeamView>>>
{
    public async ValueTask<Result<CursorPage<TeamView>>> Handle(ListTeamsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var after = query.After;

        var rows = await dbContext.Teams
            .AsNoTracking()
            .Where(team => after == null || team.Id > after.Value)
            .OrderBy(team => team.Id)
            .Select(team => new TeamView(team.Id, team.Name))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        return Result<CursorPage<TeamView>>.Success(KeysetPage.From(rows, limit, view => view.Id));
    }
}
