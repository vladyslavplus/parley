using FluentValidation;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Identity.Features.Audit.List;

public sealed class ListAuditEventsQueryValidator : AbstractValidator<ListAuditEventsQuery>
{
    public ListAuditEventsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(IdentityLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Action)
            .MaximumLength(IdentityLimits.AuditActionMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.ActorUserId)
            .Must(value => string.IsNullOrEmpty(value) || Guid.TryParse(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.From)
            .Must(AuditListRules.IsValidInstant).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.To)
            .Must(AuditListRules.IsValidInstant).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query)
            .Must(query => AuditListRules.ParseInstant(query.From) is null
                || AuditListRules.ParseInstant(query.To) is null
                || AuditListRules.ParseInstant(query.From) <= AuditListRules.ParseInstant(query.To))
            .WithName(nameof(ListAuditEventsQuery.To))
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(AuditListRules.IsValidSort).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
