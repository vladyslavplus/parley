using FluentValidation;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Sorting;
using Parley.Insights.Contracts.Rfm;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Contacts.List;

public sealed class ListContactsQueryValidator : AbstractValidator<ListContactsQuery>
{
    public ListContactsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Segment)
            .Must(value => value is null || RfmSegmentNames.All.Contains(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(ListSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query)
            .Must(query => query.CreatedFrom is null || query.CreatedTo is null || query.CreatedFrom <= query.CreatedTo)
            .WithName(nameof(ListContactsQuery.CreatedTo))
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
