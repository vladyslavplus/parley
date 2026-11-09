using FluentValidation;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>Input limits of a segment definition. Each list is bounded and each value has the shape its filter needs.</summary>
public sealed class SegmentDefinitionValidator : AbstractValidator<SegmentDefinition>
{
    public SegmentDefinitionValidator()
    {
        RuleFor(definition => definition.OwnerUserIds)
            .Must(owners => owners.Count <= CampaignsLimits.SegmentListMaximumCount && owners.All(IsIdentifier))
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(definition => definition.TagIds)
            .Must(tags => tags.Count <= CampaignsLimits.SegmentListMaximumCount && tags.All(IsIdentifier))
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(definition => definition.CountryCodes)
            .Must(codes => codes.Count <= CampaignsLimits.SegmentListMaximumCount && codes.All(IsCountryCode))
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(definition => definition.RfmSegments)
            .Must(segments => segments.Count <= CampaignsLimits.SegmentListMaximumCount
                && segments.All(segment => !string.IsNullOrWhiteSpace(segment)
                    && segment.Length <= CampaignsLimits.SegmentRfmSegmentMaximumLength))
            .WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(definition => definition.DaysSinceLastOrder)
            .Must(days => days is null || days is >= 0 and <= CampaignsLimits.SegmentDaysSinceLastOrderMaximum)
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }

    /// <summary>Null elements are rejected as input errors; they must not reach the code that reads the string.</summary>
    private static bool IsCountryCode(string? code) =>
        code is not null && code.Length == CampaignsLimits.SegmentCountryCodeLength && code.All(char.IsAsciiLetter);

    private static bool IsIdentifier(Guid id) => id != Guid.Empty;
}
