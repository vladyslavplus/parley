using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Recipients;

/// <summary>
/// One contact of a campaign's frozen audience. Written once by the prepare job and never updated: the arm, the
/// bucket, the score and the address are the values at the freeze.
/// </summary>
public sealed class Recipient : BusinessEntity
{
    public Guid CampaignId { get; set; }

    public Guid ContactId { get; set; }

    /// <summary><see cref="HoldoutArms.Treatment"/> or <see cref="HoldoutArms.Holdout"/>.</summary>
    public string Arm { get; set; } = string.Empty;

    /// <summary>Holdout bucket, 0 to 9999.</summary>
    public int AssignmentBucket { get; set; }

    public string? ScoreKind { get; set; }

    public decimal? ScoreValue { get; set; }

    public string? ModelVersion { get; set; }

    public int? RankInAudience { get; set; }

    /// <summary>True when the ranking selected the contact, across both arms.</summary>
    public bool IsTargeted { get; set; }

    public string? EmailSnapshot { get; set; }

    public string Locale { get; set; } = string.Empty;
}
