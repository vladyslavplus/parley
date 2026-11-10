using System.Text.Encodings.Web;
using Fluid;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Campaigns.Templates.Model;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Sending.Messages;

/// <summary>
/// Compiles a campaign template once and renders it per recipient through the template sandbox. The subject is
/// rendered without HTML encoding and then stripped of line breaks; the body is rendered with the HTML encoder, so a
/// contact name cannot inject markup.
/// </summary>
public sealed class CampaignMessageRenderer(TemplateSandbox sandbox)
{
    public Result<CompiledCampaignTemplate> Compile(string subjectLiquid, string bodyLiquid)
    {
        var subject = sandbox.Parse(subjectLiquid, CampaignsLimits.SubjectMaximumLength);
        if (!subject.IsSuccess)
        {
            return Result<CompiledCampaignTemplate>.Failure(subject.Error!);
        }

        var body = sandbox.Parse(bodyLiquid, CampaignsLimits.TemplateBodyMaximumLength);
        if (!body.IsSuccess)
        {
            return Result<CompiledCampaignTemplate>.Failure(body.Error!);
        }

        return Result<CompiledCampaignTemplate>.Success(new CompiledCampaignTemplate(subject.Value!, body.Value!));
    }

    public async ValueTask<Result<RenderedCampaignMessage>> RenderAsync(
        CompiledCampaignTemplate template,
        TemplateModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(model);

        var subject = await sandbox.RenderAsync(template.Subject, model, NullEncoder.Default, cancellationToken);
        if (!subject.IsSuccess)
        {
            return Result<RenderedCampaignMessage>.Failure(subject.Error!);
        }

        var html = await sandbox.RenderAsync(template.Body, model, HtmlEncoder.Default, cancellationToken);
        if (!html.IsSuccess)
        {
            return Result<RenderedCampaignMessage>.Failure(html.Error!);
        }

        return Result<RenderedCampaignMessage>.Success(
            new RenderedCampaignMessage(
                SubjectHeaderSanitizer.Sanitize(subject.Value!),
                html.Value!,
                HtmlToText.Convert(html.Value!)));
    }
}
