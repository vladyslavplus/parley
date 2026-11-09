using FluentValidation;
using Parley.Crm.Features.Orders.Shared;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Orders.List;

public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.OrderSearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(OrderSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query)
            .Must(query => query.OrderedFrom is null || query.OrderedTo is null || query.OrderedFrom <= query.OrderedTo)
            .WithName(nameof(ListOrdersQuery.OrderedTo))
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
