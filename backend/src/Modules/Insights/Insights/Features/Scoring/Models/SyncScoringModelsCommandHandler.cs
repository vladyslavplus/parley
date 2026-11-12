using Mediator;
using Parley.Insights.Features.Scoring.Client;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Models;

public sealed class SyncScoringModelsCommandHandler(ScoringModelsSync sync)
    : IRequestHandler<SyncScoringModelsCommand, Result<ScoringModelsSyncView>>
{
    public async ValueTask<Result<ScoringModelsSyncView>> Handle(SyncScoringModelsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var synced = await sync.SyncAsync(cancellationToken);
            return Result<ScoringModelsSyncView>.Success(new ScoringModelsSyncView(synced));
        }
        catch (ScoringServiceException exception)
        {
            return Result<ScoringModelsSyncView>.Failure(Error.Failure(exception.Code));
        }
    }
}
