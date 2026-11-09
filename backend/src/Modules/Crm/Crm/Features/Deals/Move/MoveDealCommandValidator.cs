using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Deals.Move;

public sealed class MoveDealCommandValidator : AbstractValidator<MoveDealCommand>
{
    public MoveDealCommandValidator()
    {
        RuleFor(command => command.StageId)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Position)
            .GreaterThanOrEqualTo(0).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.LostReason)
            .MaximumLength(CrmLimits.DealLostReasonMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
