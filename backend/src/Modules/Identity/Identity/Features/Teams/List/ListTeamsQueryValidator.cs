using FluentValidation;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Identity.Features.Teams.List;

public sealed class ListTeamsQueryValidator : AbstractValidator<ListTeamsQuery>
{
    public ListTeamsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(IdentityLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(TeamListRules.IsValidSort).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
