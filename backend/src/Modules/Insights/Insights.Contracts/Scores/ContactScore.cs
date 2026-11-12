namespace Parley.Insights.Contracts.Scores;

/// <summary>The latest score of one contact, with the model version that produced it.</summary>
/// <param name="Value">Score value; higher ranks first.</param>
/// <param name="ModelVersion">Version of the model that produced the score, copied into campaign recipients.</param>
/// <param name="Segment">The segment label the model gave the contact, when the kind has one (uplift: persuadable, sure_thing, ...).</param>
public sealed record ContactScore(decimal Value, string ModelVersion, string? Segment = null);
