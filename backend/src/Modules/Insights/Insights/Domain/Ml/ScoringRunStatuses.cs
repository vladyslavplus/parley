namespace Parley.Insights.Domain.Ml;

/// <summary>Lifecycle of a scoring run. A run is queued by the API, runs in the job, and ends completed or failed.</summary>
public static class ScoringRunStatuses
{
    public const string Queued = "queued";

    public const string Running = "running";

    public const string Completed = "completed";

    public const string Failed = "failed";

    /// <summary>
    /// Statuses of a run that still holds the organization's slot for its kind. A list, not an array: an array inside an EF
    /// query binds the span overload of <c>Contains</c>, which the provider cannot translate.
    /// </summary>
    public static readonly IReadOnlyList<string> Active = [Queued, Running];
}
