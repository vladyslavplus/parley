using FluentValidation;
using Parley.Identity.Shared;
using Parley.Identity.Shared.Validation;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Invitations.Invite;

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(command => command.Email).Email();
        RuleFor(command => command.RoleCode)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.RoleCodeMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Culture).SupportedLocale();
    }
}
