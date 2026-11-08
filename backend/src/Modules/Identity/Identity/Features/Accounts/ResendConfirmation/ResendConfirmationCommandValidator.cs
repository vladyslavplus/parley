using FluentValidation;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Accounts.ResendConfirmation;

public sealed class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationCommandValidator()
    {
        RuleFor(command => command.Email).Email();
    }
}
