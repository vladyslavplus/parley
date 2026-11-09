using FluentValidation;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Identity.Features.Invitations.List;

public sealed class ListInvitationsQueryValidator : AbstractValidator<ListInvitationsQuery>
{
    public ListInvitationsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(IdentityLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.RoleCode)
            .MaximumLength(IdentityLimits.RoleCodeMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(InvitationListRules.IsValidSort).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
