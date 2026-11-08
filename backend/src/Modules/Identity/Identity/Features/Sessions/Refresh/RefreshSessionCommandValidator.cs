using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Sessions.Refresh;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(command => command.RefreshToken).Token(IdentityLimits.TokenMaximumLength);
    }
}
