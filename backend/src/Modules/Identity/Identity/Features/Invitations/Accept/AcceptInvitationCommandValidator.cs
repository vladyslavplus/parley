using FluentValidation;
using Microsoft.Extensions.Options;
using Parley.Identity.Options.Auth;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Invitations.Accept;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator(IOptions<AuthOptions> options)
    {
        RuleFor(command => command.Token).Token(IdentityLimits.TokenMaximumLength);
        RuleFor(command => command.Password).Password(options.Value.PasswordMinimumLength);
        RuleFor(command => command.Culture).SupportedLocale();
    }
}
