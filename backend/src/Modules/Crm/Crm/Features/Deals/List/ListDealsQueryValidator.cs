using FluentValidation;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Deals;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Deals.List;

public sealed class ListDealsQueryValidator : AbstractValidator<ListDealsQuery>
{
    public ListDealsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(DealSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Status)
            .Must(value => value is null || DealStatuses.All.Contains(value))
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
