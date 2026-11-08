using FluentValidation;
using Microsoft.Extensions.Options;
using Parley.Identity.Options.Auth;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Accounts.Register;

public sealed class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator(IOptions<AuthOptions> options)
    {
        RuleFor(command => command.Email).Email();
        RuleFor(command => command.Password).Password(options.Value.PasswordMinimumLength);
        RuleFor(command => command.OrganizationName)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.OrganizationNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
