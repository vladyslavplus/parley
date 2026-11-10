using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Update;

public sealed class UpdateSegmentCommandHandler(CampaignsDbContext dbContext)
    : IRequestHandler<UpdateSegmentCommand, Result<SegmentView>>
{
    public async ValueTask<Result<SegmentView>> Handle(UpdateSegmentCommand command, CancellationToken cancellationToken)
    {
        var segment = await SegmentLookup.FindLiveTrackedAsync(dbContext, command.Id, cancellationToken);
        if (segment is null)
        {
            return Result<SegmentView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound));
        }

        segment.Name = command.Name.Trim();
        segment.Definition = SegmentDefinitionRules.Normalize(command.Definition);

        // The stale-write check: the UPDATE matches the client's row version, so a row changed since then affects nothing.
        var entry = dbContext.Entry(segment);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SegmentView>.Failure(Error.Conflict(CampaignsErrorCodes.SegmentVersionConflict));
        }

        var view = await SegmentLookup.FindLiveViewAsync(dbContext, segment.Id, cancellationToken);
        return view is null
            ? Result<SegmentView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound))
            : Result<SegmentView>.Success(view);
    }
}
