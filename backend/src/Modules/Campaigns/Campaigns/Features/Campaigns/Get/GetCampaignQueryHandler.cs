using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Get;

/// <summary>
/// Reads the campaign through the organization filter of the database context. A campaign of another organization is
/// not found, so it answers the same way as an unknown identifier.
/// </summary>
public sealed class GetCampaignQueryHandler(CampaignsDbContext dbContext) : IRequestHandler<GetCampaignQuery, Result<CampaignView>>
{
    public async ValueTask<Result<CampaignView>> Handle(GetCampaignQuery query, CancellationToken cancellationToken)
    {
        var view = await CampaignLookup.FindViewAsync(dbContext, query.Id, cancellationToken);
        return view is null
            ? Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound))
            : Result<CampaignView>.Success(view);
    }
}
