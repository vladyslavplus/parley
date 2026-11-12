using Mediator;
using Parley.Insights.Features.MlSettings.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.MlSettings.Get;

public sealed class GetMlSettingsQueryHandler(InsightsDbContext dbContext)
    : IRequestHandler<GetMlSettingsQuery, Result<MlSettingsView>>
{
    public async ValueTask<Result<MlSettingsView>> Handle(GetMlSettingsQuery query, CancellationToken cancellationToken) =>
        Result<MlSettingsView>.Success(await MlSettingsLookup.FindViewAsync(dbContext, cancellationToken));
}
