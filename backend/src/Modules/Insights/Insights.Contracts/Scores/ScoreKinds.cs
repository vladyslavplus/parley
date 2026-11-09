namespace Parley.Insights.Contracts.Scores;

/// <summary>Score kinds a campaign can rank its audience by.</summary>
public static class ScoreKinds
{
    /// <summary>Individual treatment effect from the uplift model.</summary>
    public const string Uplift = "uplift";

    /// <summary>Recency, frequency and monetary tier ranking.</summary>
    public const string Rfm = "rfm";
}
