namespace Parley.Insights.Domain.Ml;

/// <summary>Where a model came from, as the scoring service reports it. Provenance travels with every score into the UI.</summary>
public static class ScoringProvenances
{
    /// <summary>A fixture model that boots the service and validates the pipeline; its scores are not an effect estimate.</summary>
    public const string Stub = "stub";

    /// <summary>A model trained on the semi-synthetic CRM fixture: honest labels, no real effect of real email.</summary>
    public const string SemiSynthetic = "semi_synthetic";

    /// <summary>A model trained on a public dataset, such as the CLV model on Online Retail II.</summary>
    public const string PublicDataset = "public_dataset";
}
