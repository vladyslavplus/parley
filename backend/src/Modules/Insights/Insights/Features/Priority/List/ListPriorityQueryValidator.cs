using FluentValidation;
using Parley.Insights.Contracts.Rfm;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Insights.Features.Priority.List;

public sealed class ListPriorityQueryValidator : AbstractValidator<ListPriorityQuery>
{
    public ListPriorityQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Segment)
            .Must(value => value is null || RfmSegmentNames.All.Contains(value)).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
