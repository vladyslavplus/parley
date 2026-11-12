namespace Parley.Insights.Features.Scoring.Client;

/// <summary>
/// A call to the scoring service failed. <see cref="Code"/> is a stable insights code the run stores and the UI localizes;
/// the service's own detail text is never carried here, so no request content reaches a log or a response.
/// </summary>
public sealed class ScoringServiceException(string code) : Exception(code)
{
    /// <summary>The stable insights code of the failure.</summary>
    public string Code { get; } = code;
}
