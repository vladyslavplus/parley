using FluentValidation;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Campaigns.Features.Templates.List;

public sealed class ListTemplatesQueryValidator : AbstractValidator<ListTemplatesQuery>
{
    public ListTemplatesQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(CampaignsLimits.NameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
