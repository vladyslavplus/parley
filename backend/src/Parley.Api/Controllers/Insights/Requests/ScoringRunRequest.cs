namespace Parley.Api.Controllers.Insights.Requests;

/// <summary>Body of a scoring run start.</summary>
/// <param name="Kind">The score kind to run: uplift or clv; required.</param>
public sealed record ScoringRunRequest(string? Kind);
