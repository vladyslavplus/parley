using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Shared;

/// <summary>Checks the subject and body of a template with the sandbox before anything is saved.</summary>
internal static class TemplateContentRules
{
    public static Result Validate(TemplateSandbox sandbox, string subjectLiquid, string bodyLiquid)
    {
        var subject = sandbox.Parse(subjectLiquid, CampaignsLimits.SubjectMaximumLength);
        if (!subject.IsSuccess)
        {
            return Result.Failure(subject.Error!);
        }

        var body = sandbox.Parse(bodyLiquid, CampaignsLimits.TemplateBodyMaximumLength);
        return body.IsSuccess ? Result.Success() : Result.Failure(body.Error!);
    }
}
