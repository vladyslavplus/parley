using FluentValidation;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Campaigns.Features.Campaigns.Update;

public sealed class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CampaignsLimits.NameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TemplateId)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TargetingMode)
            .Must(CampaignTargetingModes.IsKnown).WithErrorCode(CampaignsErrorCodes.CampaignTargetingModeInvalid);
        RuleFor(command => command.Version)
            .GreaterThan(0u).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
