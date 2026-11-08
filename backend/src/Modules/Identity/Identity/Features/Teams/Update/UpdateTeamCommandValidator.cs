using FluentValidation;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Teams.Update;

public sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(command => command.Name.Trim())
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.TeamNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
