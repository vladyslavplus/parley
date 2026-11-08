using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Accounts.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Token).Token(IdentityLimits.ConfirmationTokenMaximumLength);
    }
}
