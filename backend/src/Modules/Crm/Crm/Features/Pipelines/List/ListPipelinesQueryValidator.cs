using FluentValidation;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Crm.Features.Pipelines.List;

public sealed class ListPipelinesQueryValidator : AbstractValidator<ListPipelinesQuery>
{
    public ListPipelinesQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Search)
            .MaximumLength(CrmLimits.SearchMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(PipelineSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
