namespace Parley.Insights.Domain.Rfm;

/// <summary>
/// The RFM score a contact gets for campaign targeting. It is a tier code, not a learned value: recency weighs most, then
/// frequency, then monetary, so a higher code means a better contact.
/// </summary>
public static class RfmScoring
{
    /// <summary>Model version stamped on every RFM score, so a campaign records which rule ranked its audience.</summary>
    public const string ModelVersion = "rfm-v1";

    /// <summary>Weight of the recency tier in the score code.</summary>
    public const decimal RecencyWeight = 100m;

    /// <summary>Weight of the frequency tier in the score code.</summary>
    public const decimal FrequencyWeight = 10m;

    /// <summary>The score code of a contact: R*100 + F*10 + M, between 111 and 555.</summary>
    public static decimal ScoreValue(RfmTiers tiers) =>
        tiers.R * RecencyWeight + tiers.F * FrequencyWeight + tiers.M;
}
