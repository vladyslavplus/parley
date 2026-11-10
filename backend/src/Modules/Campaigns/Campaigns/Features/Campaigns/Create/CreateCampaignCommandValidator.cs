using FluentValidation;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Campaigns.Features.Campaigns.Create;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CampaignsLimits.NameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TemplateId)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TargetingMode)
            .Must(CampaignTargetingModes.IsKnown).WithErrorCode(CampaignsErrorCodes.CampaignTargetingModeInvalid);
    }
}
