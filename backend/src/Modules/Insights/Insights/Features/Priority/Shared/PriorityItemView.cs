namespace Parley.Insights.Features.Priority.Shared;

/// <summary>One contact of the priority list, as the list endpoint returns it. Names only: no address, no statistic.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="DisplayName">First and last name of the contact.</param>
/// <param name="CompanyName">Name of the contact's company; null when there is none.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="Rank">Rank among the owner's contacts on the build day.</param>
/// <param name="Value">Priority value; higher ranks first. Null unless the caller holds the ML view permission: the value of an ML row is derived from the scores.</param>
/// <param name="Mode">Scoring mode of the value.</param>
/// <param name="Segment">RFM segment code.</param>
/// <param name="UpliftValue">Uplift the model estimated for the contact; null in the RFM mode, for a contact without a score, and for a caller without the ML view permission.</param>
/// <param name="UpliftSegment">Uplift segment the model gave the contact; visible to every caller with the insights permission, unlike the uplift value. Null in the RFM mode and for a contact without a score.</param>
/// <param name="ReasonCodes">Reason codes that explain the value.</param>
/// <param name="SuggestedAction">Next action code.</param>
public sealed record PriorityItemView(
    Guid ContactId,
    string DisplayName,
    string? CompanyName,
    Guid? OwnerUserId,
    int Rank,
    decimal? Value,
    string Mode,
    string Segment,
    decimal? UpliftValue,
    string? UpliftSegment,
    IReadOnlyList<string> ReasonCodes,
    string SuggestedAction);
