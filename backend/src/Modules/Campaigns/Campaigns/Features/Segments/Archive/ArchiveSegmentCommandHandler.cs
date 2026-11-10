using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Archive;

public sealed class ArchiveSegmentCommandHandler(CampaignsDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ArchiveSegmentCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveSegmentCommand command, CancellationToken cancellationToken)
    {
        var segment = await SegmentLookup.FindLiveTrackedAsync(dbContext, command.Id, cancellationToken);
        if (segment is null)
        {
            return Result.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound));
        }

        segment.ArchivedAt = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict(CampaignsErrorCodes.SegmentVersionConflict));
        }

        return Result.Success();
    }
}
