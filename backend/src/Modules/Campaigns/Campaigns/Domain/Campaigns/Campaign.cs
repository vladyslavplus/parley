using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Campaigns;

/// <summary>
/// A campaign: one template, one optional segment, and the holdout and targeting settings. Its audience is frozen once
/// by the prepare job; after the freeze the recipients never change.
/// </summary>
public sealed class Campaign : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid TemplateId { get; set; }

    /// <summary>Segment the audience comes from; null means every contact that has an address.</summary>
    public Guid? SegmentId { get; set; }

    public string Status { get; set; } = CampaignStatuses.Draft;

    /// <summary>Share of the audience kept out of the email, in basis points.</summary>
    public int HoldoutShareBps { get; set; }

    /// <summary>Seed of the holdout assignment. Set once at creation to the campaign id, so each campaign assigns independently.</summary>
    public string HoldoutSeed { get; set; } = string.Empty;

    public string TargetingMode { get; set; } = CampaignTargetingModes.All;

    /// <summary>Share of the audience the score ranking targets, in basis points; null for <see cref="CampaignTargetingModes.All"/>.</summary>
    public int? TargetingShareBps { get; set; }

    public int AttributionWindowDays { get; set; } = CampaignDefaults.AttributionWindowDays;

    public string ConversionRule { get; set; } = CampaignConversionRules.AnyOrder;

    /// <summary>
    /// The instant the audience is ranked and filtered at. Pinned when the campaign enters <c>preparing</c>, so every run of
    /// the freeze, resumed or repeated, sees the same CRM day counts.
    /// </summary>
    public DateTime? PrepareAsOf { get; set; }

    /// <summary>The commit point of the freeze. Null until the recipients are complete; set once and never cleared.</summary>
    public DateTime? AudienceFrozenAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? StartedBy { get; set; }

    public string Source { get; set; } = CampaignSources.User;
}
