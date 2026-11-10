using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Templates;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Update;

public sealed class UpdateTemplateCommandHandler(CampaignsDbContext dbContext, TemplateSandbox sandbox)
    : IRequestHandler<UpdateTemplateCommand, Result<TemplateView>>
{
    public async ValueTask<Result<TemplateView>> Handle(UpdateTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await TemplateLookup.FindLiveTrackedAsync(dbContext, command.Id, cancellationToken);
        if (template is null)
        {
            return Result<TemplateView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        var validation = TemplateContentRules.Validate(sandbox, command.SubjectLiquid, command.BodyLiquid);
        if (!validation.IsSuccess)
        {
            return Result<TemplateView>.Failure(validation.Error!);
        }

        template.Name = command.Name.Trim();
        template.SubjectLiquid = command.SubjectLiquid;
        template.BodyLiquid = command.BodyLiquid;
        template.Locale = command.Locale;

        // The stale-write check: the UPDATE matches the client's row version, so a row changed since then affects nothing.
        var entry = dbContext.Entry(template);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TemplateView>.Failure(Error.Conflict(CampaignsErrorCodes.TemplateVersionConflict));
        }

        var view = await TemplateLookup.FindLiveViewAsync(dbContext, template.Id, cancellationToken);
        return view is null
            ? Result<TemplateView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound))
            : Result<TemplateView>.Success(view);
    }
}
