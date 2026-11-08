using FluentValidation;
using Parley.Identity.Options.Auth;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Sessions.SignIn;

public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    private const int PasswordMaximumLength = AuthDefaults.MaximumPasswordLength;

    public SignInCommandValidator()
    {
        RuleFor(command => command.Email).Email();
        RuleFor(command => command.Password)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(PasswordMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
