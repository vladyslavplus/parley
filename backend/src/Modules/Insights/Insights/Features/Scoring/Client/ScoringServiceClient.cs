using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Options;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>
/// Typed HTTP client of the scoring service. Every call carries the shared key and a fresh request id. The retries and the
/// timeouts come from the standard resilience handler the module registers. Scoring is pure computation, so a retried call
/// returns the same scores. Calls run from background jobs only: no user request waits on the service.
/// </summary>
public sealed class ScoringServiceClient(HttpClient http, IOptions<InsightsScoringOptions> options)
{
    /// <summary>Header carrying the shared key. The key is a secret and is never logged.</summary>
    private const string KeyHeader = "X-Parley-Ml-Key";

    /// <summary>Header carrying the request id the service echoes in its logs and error bodies.</summary>
    private const string RequestIdHeader = "X-Request-Id";

    private const string UpliftPath = "score/uplift";

    private const string ClvPath = "score/clv";

    private const string ModelsPath = "models";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<UpliftScoreResponse> ScoreUpliftAsync(UpliftScoreRequest request, CancellationToken cancellationToken) =>
        SendAsync<UpliftScoreResponse>(HttpMethod.Post, UpliftPath, request, cancellationToken);

    public Task<ClvScoreResponse> ScoreClvAsync(ClvScoreRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClvScoreResponse>(HttpMethod.Post, ClvPath, request, cancellationToken);

    public Task<ScoringModelsResponse> ReadModelsAsync(CancellationToken cancellationToken) =>
        SendAsync<ScoringModelsResponse>(HttpMethod.Get, ModelsPath, body: null, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var key = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            // Refused before any call: a run without a key fails with the stable code; no request leaves the host.
            throw new ScoringServiceException(InsightsErrorCodes.ScoringServiceRejected);
        }

        using var message = new HttpRequestMessage(method, path);
        message.Headers.Add(KeyHeader, key);
        message.Headers.Add(RequestIdHeader, Guid.NewGuid().ToString("D"));
        if (body is not null)
        {
            message.Content = JsonContent.Create(body, options: JsonOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ScoringServiceException(InsightsErrorCodes.ScoringServiceUnavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The per-attempt or total timeout of the resilience handler, not a cancelled run.
            throw new ScoringServiceException(InsightsErrorCodes.ScoringServiceUnavailable);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ScoringServiceException(ScoringRunRules.ErrorCodeOfStatus((int)response.StatusCode));
            }

            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return result ?? throw new ScoringServiceException(InsightsErrorCodes.ScoringServiceUnavailable);
        }
    }
}
