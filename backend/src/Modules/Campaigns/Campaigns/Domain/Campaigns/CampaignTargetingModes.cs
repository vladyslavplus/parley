using Parley.Insights.Contracts.Scores;

namespace Parley.Campaigns.Domain.Campaigns;

/// <summary>How the audience is targeted. Only <see cref="All"/> needs no score.</summary>
public static class CampaignTargetingModes
{
    /// <summary>Every recipient is targeted.</summary>
    public const string All = "all";

    /// <summary>The top share of the audience by uplift score is targeted.</summary>
    public const string TopUplift = "top_uplift";

    /// <summary>The top share of the audience by RFM score is targeted.</summary>
    public const string TopRfm = "top_rfm";

    public static bool IsKnown(string mode) => mode is All or TopUplift or TopRfm;

    /// <summary>True when the mode ranks by a score, so it needs scores and the insights permission.</summary>
    public static bool RanksByScore(string mode) => mode is TopUplift or TopRfm;

    /// <summary>The score kind a ranking mode uses; null for <see cref="All"/>.</summary>
    public static string? ScoreKindOf(string mode) => mode switch
    {
        TopUplift => ScoreKinds.Uplift,
        TopRfm => ScoreKinds.Rfm,
        _ => null,
    };
}
