using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Sessions.SignOut;

public sealed class SignOutCommandValidator : AbstractValidator<SignOutCommand>
{
    public SignOutCommandValidator()
    {
        RuleFor(command => command.RefreshToken).Token(IdentityLimits.TokenMaximumLength);
    }
}
