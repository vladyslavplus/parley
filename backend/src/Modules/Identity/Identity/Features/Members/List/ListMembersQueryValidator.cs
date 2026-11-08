using FluentValidation;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Identity.Features.Members.List;

public sealed class ListMembersQueryValidator : AbstractValidator<ListMembersQuery>
{
    public ListMembersQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(IdentityLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.RoleCode)
            .MaximumLength(IdentityLimits.RoleCodeMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.TeamId)
            .Must(value => string.IsNullOrEmpty(value) || Guid.TryParse(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Status)
            .Must(MemberListRules.IsValidStatus).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(MemberListRules.IsValidSort).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
