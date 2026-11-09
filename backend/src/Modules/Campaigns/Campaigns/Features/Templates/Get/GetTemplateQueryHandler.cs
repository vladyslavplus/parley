using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Get;

public sealed class GetTemplateQueryHandler(CampaignsDbContext dbContext) : IRequestHandler<GetTemplateQuery, Result<TemplateView>>
{
    public async ValueTask<Result<TemplateView>> Handle(GetTemplateQuery query, CancellationToken cancellationToken)
    {
        var template = await TemplateLookup.FindLiveViewAsync(dbContext, query.Id, cancellationToken);
        return template is null
            ? Result<TemplateView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound))
            : Result<TemplateView>.Success(template);
    }
}
