using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Organizations.Update;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(command => command.Name.Trim())
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.OrganizationNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.TimeZone).TimeZone();
        RuleFor(command => command.DefaultLocale).SupportedLocale();
        RuleFor(command => command.Currency).Currency();
    }
}
