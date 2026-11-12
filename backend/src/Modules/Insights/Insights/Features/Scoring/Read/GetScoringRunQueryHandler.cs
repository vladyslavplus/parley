using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Read;

public sealed class GetScoringRunQueryHandler(InsightsDbContext dbContext) : IRequestHandler<GetScoringRunQuery, Result<ScoringRunView>>
{
    public async ValueTask<Result<ScoringRunView>> Handle(GetScoringRunQuery query, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScoringRuns.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == query.ScoringRunId, cancellationToken);
        return run is null
            ? Result<ScoringRunView>.Failure(Error.NotFound(InsightsErrorCodes.ScoringRunNotFound))
            : Result<ScoringRunView>.Success(ScoringRunView.From(run));
    }
}
