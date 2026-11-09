using FluentValidation;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Campaigns.Features.Segments.List;

public sealed class ListSegmentsQueryValidator : AbstractValidator<ListSegmentsQuery>
{
    public ListSegmentsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(CampaignsLimits.SegmentNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
