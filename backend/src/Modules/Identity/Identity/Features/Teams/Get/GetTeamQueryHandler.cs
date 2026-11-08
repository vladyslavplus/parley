using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.Get;

public sealed class GetTeamQueryHandler(IdentityDbContext dbContext) : IRequestHandler<GetTeamQuery, Result<TeamView>>
{
    public async ValueTask<Result<TeamView>> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        var view = await dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == query.TeamId)
            .Select(team => new TeamView(team.Id, team.Name))
            .FirstOrDefaultAsync(cancellationToken);
        return view is null
            ? Result<TeamView>.Failure(Error.NotFound(IdentityErrorCodes.TeamNotFound))
            : Result<TeamView>.Success(view);
    }
}
