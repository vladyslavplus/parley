using FluentValidation;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;

namespace Parley.Campaigns.Features.Campaigns.List;

public sealed class ListCampaignsQueryValidator : AbstractValidator<ListCampaignsQuery>
{
    public ListCampaignsQueryValidator()
    {
        RuleFor(query => query.After)
            .Must(value => value is null || KeysetCursor.TryDecode(value, out _)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Q)
            .MaximumLength(CampaignsLimits.NameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Status)
            .Must(value => string.IsNullOrEmpty(value) || CampaignStatuses.IsKnown(value)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Sort)
            .Must(CampaignListSortFields.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(query => query.Order)
            .Must(SortDirections.IsValid).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
