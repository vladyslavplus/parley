using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Segments.Create;

public sealed class CreateSegmentCommandHandler(CampaignsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateSegmentCommand, Result<SegmentView>>
{
    public async ValueTask<Result<SegmentView>> Handle(CreateSegmentCommand command, CancellationToken cancellationToken)
    {
        var segment = new Segment
        {
            OrganizationId = tenantContext.OrganizationId,
            Name = command.Name.Trim(),
            Definition = SegmentDefinitionRules.Normalize(command.Definition),
        };
        dbContext.Segments.Add(segment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var view = await SegmentLookup.FindLiveViewAsync(dbContext, segment.Id, cancellationToken);
        return view is null
            ? Result<SegmentView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound))
            : Result<SegmentView>.Success(view);
    }
}
