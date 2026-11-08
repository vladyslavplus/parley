using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Organizations.Switch;

public sealed class SwitchOrganizationCommandValidator : AbstractValidator<SwitchOrganizationCommand>
{
    public SwitchOrganizationCommandValidator()
    {
        RuleFor(command => command.RefreshToken).Token(IdentityLimits.TokenMaximumLength);
    }
}
