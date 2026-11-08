using FluentValidation;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Identity.Features.Teams.Create;

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(command => command.Name.Trim())
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(IdentityLimits.TeamNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
