namespace Parley.Campaigns.Features.Campaigns.Recipients;

/// <summary>
/// One recipient of a campaign as the recipients list shows it. The conversion is the stored outcome only: no rate and no
/// lift, so the page never recomputes a statistic. A contact outside the caller's contacts scope, or one that no longer
/// exists, shows no name, no conversion and no revenue: <see cref="NameVisible"/> is false and the conversion reads
/// <c>not_visible</c>. The address is present only when the caller may see the contact's address through the contacts
/// permission at organization scope; otherwise it is null and <see cref="EmailVisible"/> is false.
/// </summary>
/// <param name="RecipientId">The recipient row.</param>
/// <param name="ContactId">The contact.</param>
/// <param name="ContactName">Display name of the contact; null when it is not visible to the caller.</param>
/// <param name="NameVisible">Whether the name, the conversion and the revenue of this contact are shown to the caller.</param>
/// <param name="Arm"><c>treatment</c> or <c>holdout</c>.</param>
/// <param name="IsTargeted">Whether the recipient is in the targeted population of the comparison.</param>
/// <param name="SendStatus">Status of the send; null for holdout and not targeted recipients.</param>
/// <param name="Conversion">One of <see cref="CampaignRecipientConversions"/>.</param>
/// <param name="FirstConversionAt">First conversion instant in the window; null without a conversion, an outcome, or visibility.</param>
/// <param name="Revenue">Net revenue in the window; null without an outcome, or without visibility.</param>
/// <param name="EmailVisible">Whether the address below is shown.</param>
/// <param name="Email">The address frozen with the audience, when visible; otherwise null.</param>
public sealed record CampaignRecipientView(
    Guid RecipientId,
    Guid ContactId,
    string? ContactName,
    bool NameVisible,
    string Arm,
    bool IsTargeted,
    string? SendStatus,
    string Conversion,
    DateTime? FirstConversionAt,
    decimal? Revenue,
    bool EmailVisible,
    string? Email);
