using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Templates;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Templates;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Templates.Create;

public sealed class CreateTemplateCommandHandler(CampaignsDbContext dbContext, ITenantContext tenantContext, TemplateSandbox sandbox)
    : IRequestHandler<CreateTemplateCommand, Result<TemplateView>>
{
    public async ValueTask<Result<TemplateView>> Handle(CreateTemplateCommand command, CancellationToken cancellationToken)
    {
        var validation = TemplateContentRules.Validate(sandbox, command.SubjectLiquid, command.BodyLiquid);
        if (!validation.IsSuccess)
        {
            return Result<TemplateView>.Failure(validation.Error!);
        }

        var template = new EmailTemplate
        {
            OrganizationId = tenantContext.OrganizationId,
            Name = command.Name.Trim(),
            SubjectLiquid = command.SubjectLiquid,
            BodyLiquid = command.BodyLiquid,
            Locale = command.Locale,
        };
        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        var view = await TemplateLookup.FindLiveViewAsync(dbContext, template.Id, cancellationToken);
        return view is null
            ? Result<TemplateView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound))
            : Result<TemplateView>.Success(view);
    }
}
