using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Archive;

public sealed class ArchiveTemplateCommandHandler(CampaignsDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ArchiveTemplateCommand, Result>
{
    public async ValueTask<Result> Handle(ArchiveTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await TemplateLookup.FindLiveTrackedAsync(dbContext, command.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        template.ArchivedAt = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict(CampaignsErrorCodes.TemplateVersionConflict));
        }

        return Result.Success();
    }
}
