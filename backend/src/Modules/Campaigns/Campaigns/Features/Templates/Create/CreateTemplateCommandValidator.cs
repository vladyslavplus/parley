using FluentValidation;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;

namespace Parley.Campaigns.Features.Templates.Create;

public sealed class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CampaignsLimits.NameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.SubjectLiquid)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.BodyLiquid)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Locale)
            .Must(value => value is CultureCodes.English or CultureCodes.Ukrainian).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
