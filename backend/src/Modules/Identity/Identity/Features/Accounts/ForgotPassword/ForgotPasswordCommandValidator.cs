using FluentValidation;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Accounts.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email).Email();
    }
}
