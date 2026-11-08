using FluentValidation;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.CustomFields.List;

public sealed class ListCustomFieldsQueryValidator : AbstractValidator<ListCustomFieldsQuery>
{
    public ListCustomFieldsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.EntityType)
            .Must(value => string.IsNullOrEmpty(value) || CustomFieldRules.IsValidEntityType(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.FieldType)
            .Must(value => string.IsNullOrEmpty(value) || CustomFieldRules.IsValidFieldType(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(CustomFieldListRules.IsValidSort).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
