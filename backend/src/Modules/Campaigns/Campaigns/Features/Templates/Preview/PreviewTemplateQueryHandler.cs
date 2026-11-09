using System.Text.Encodings.Web;
using Fluid;
using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Campaigns.Templates.Model;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Preview;

public sealed class PreviewTemplateQueryHandler(CampaignsDbContext dbContext, TemplateSandbox sandbox)
    : IRequestHandler<PreviewTemplateQuery, Result<TemplatePreviewView>>
{
    public async ValueTask<Result<TemplatePreviewView>> Handle(PreviewTemplateQuery query, CancellationToken cancellationToken)
    {
        var template = await TemplateLookup.FindLiveViewAsync(dbContext, query.Id, cancellationToken);
        if (template is null)
        {
            return Result<TemplatePreviewView>.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        var subject = sandbox.Parse(template.SubjectLiquid, CampaignsLimits.SubjectMaximumLength);
        if (!subject.IsSuccess)
        {
            return Result<TemplatePreviewView>.Failure(subject.Error!);
        }

        var body = sandbox.Parse(template.BodyLiquid, CampaignsLimits.TemplateBodyMaximumLength);
        if (!body.IsSuccess)
        {
            return Result<TemplatePreviewView>.Failure(body.Error!);
        }

        var renderedSubject = await sandbox.RenderAsync(subject.Value!, TemplateSampleData.Model, NullEncoder.Default, cancellationToken);
        if (!renderedSubject.IsSuccess)
        {
            return Result<TemplatePreviewView>.Failure(renderedSubject.Error!);
        }

        var renderedBody = await sandbox.RenderAsync(body.Value!, TemplateSampleData.Model, HtmlEncoder.Default, cancellationToken);
        return renderedBody.IsSuccess
            ? Result<TemplatePreviewView>.Success(new TemplatePreviewView(renderedSubject.Value!, renderedBody.Value!))
            : Result<TemplatePreviewView>.Failure(renderedBody.Error!);
    }
}
