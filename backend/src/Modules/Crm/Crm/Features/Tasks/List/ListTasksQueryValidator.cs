using FluentValidation;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Tasks;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Tasks.List;

public sealed class ListTasksQueryValidator : AbstractValidator<ListTasksQuery>
{
    public ListTasksQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(TaskSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Status)
            .Must(value => value is null || TaskStatuses.All.Contains(value))
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
