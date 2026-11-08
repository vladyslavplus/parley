using FluentValidation;
using Microsoft.Extensions.Options;
using Parley.Identity.Options.Auth;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Accounts.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(IOptions<AuthOptions> options)
    {
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Token).Token(IdentityLimits.ConfirmationTokenMaximumLength);
        RuleFor(command => command.NewPassword).Password(options.Value.PasswordMinimumLength);
    }
}
