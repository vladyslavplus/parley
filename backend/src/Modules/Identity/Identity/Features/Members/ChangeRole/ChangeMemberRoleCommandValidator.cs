using FluentValidation;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Members.ChangeRole;

public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(command => command.RoleCode)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.RoleCodeMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
