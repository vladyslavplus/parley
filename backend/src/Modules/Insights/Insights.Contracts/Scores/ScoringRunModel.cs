namespace Parley.Insights.Contracts.Scores;

/// <summary>The model a scoring run pinned, as the run and the registry describe it.</summary>
/// <param name="ModelName">Registry name of the model, for example <c>crm_uplift</c>.</param>
/// <param name="ModelVersion">Version the run scored with.</param>
/// <param name="Provenance">Where the model came from, as the registry holds it.</param>
public sealed record ScoringRunModel(string ModelName, string ModelVersion, string Provenance);
