using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Parley.IntegrationTests.Insights.Scoring.Support;

/// <summary>
/// In-process stand-in for the scoring service, answering the contract's routes. A test scripts it: which subjects are
/// rejected, whether every item is rejected, which status a score call is refused with, and which contacts are marked
/// do-not-disturb. Every uplift request is recorded, so a test can check the batching and the pinned model version.
/// </summary>
public sealed class FakeScoringService : HttpMessageHandler
{
    public const string ModelName = "crm_uplift";

    public const string ModelVersion = "crm_uplift-test-v1";

    public const string KeyHeader = "X-Parley-Ml-Key";

    public const int FeatureSchemaVersion = 1;

    public const decimal Uplift = 0.2m;

    private const string ExpectedKey = "test-key";

    private const string ProvenanceTrained = "semi_synthetic";

    private const string SegmentPersuadable = "persuadable";

    private const string SegmentDoNotDisturb = "do_not_disturb";

    private const string RejectionCode = "ml.request.invalid_features";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly List<int> _uplifts = [];

    private readonly List<string?> _requestedVersions = [];

    /// <summary>Subject ids (contact ids) the service rejects.</summary>
    public HashSet<string> RejectedSubjects { get; } = [];

    /// <summary>Subject ids (contact ids) the service marks do-not-disturb.</summary>
    public HashSet<string> DoNotDisturbSubjects { get; } = [];

    /// <summary>When true, every item of every score request is rejected.</summary>
    public bool RejectAll { get; set; }

    /// <summary>When set, score requests are refused with this status instead of scored.</summary>
    public HttpStatusCode? RefuseWith { get; set; }

    /// <summary>When set, the model registry request is refused with this status.</summary>
    public HttpStatusCode? RefuseModelsWith { get; set; }

    /// <summary>When true, the registry answers with no models: the service no longer serves what it served before.</summary>
    public bool HideModels { get; set; }

    /// <summary>
    /// Runs once before the next uplift response. A test uses it to change the run while the runner works, for example to
    /// end the run as failed before the swap.
    /// </summary>
    public Func<Task>? BeforeUpliftResponseAsync { get; set; }

    /// <summary>Number of items of each uplift request, in order.</summary>
    public IReadOnlyList<int> UpliftBatchSizes => _uplifts.ToArray();

    /// <summary>The model version each uplift request asked for, in order; null when the request left it to the service.</summary>
    public IReadOnlyList<string?> RequestedVersions => _requestedVersions.ToArray();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValues(KeyHeader, out var keys) || !keys.Contains(ExpectedKey))
        {
            return Error(HttpStatusCode.Unauthorized, "ml.auth.invalid_key");
        }

        var path = request.RequestUri!.AbsolutePath.Trim('/');
        return (request.Method.Method, path) switch
        {
            ("GET", "models") => RefuseModelsWith is { } refusal
                ? Error(refusal, "ml.models.unavailable")
                : Json(HttpStatusCode.OK, HideModels ? EmptyModels() : Models()),
            ("POST", "score/uplift") => await ScoreUpliftAsync(request, cancellationToken),
            ("POST", "score/clv") => Error(HttpStatusCode.NotFound, "ml.score.unknown_model_version"),
            _ => Error(HttpStatusCode.NotFound, "ml.request.not_found"),
        };
    }

    private async Task<HttpResponseMessage> ScoreUpliftAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (RefuseWith is { } refusal)
        {
            return Error(refusal, refusal == HttpStatusCode.NotFound ? "ml.score.unknown_model_version" : "ml.score.feature_schema_mismatch");
        }

        if (BeforeUpliftResponseAsync is { } hook)
        {
            BeforeUpliftResponseAsync = null;
            await hook();
        }

        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var items = root.GetProperty("items").EnumerateArray().ToList();
        var requestedVersion = root.TryGetProperty("model_version", out var version) && version.ValueKind == JsonValueKind.String
            ? version.GetString()
            : null;
        _uplifts.Add(items.Count);
        _requestedVersions.Add(requestedVersion);

        var scores = new List<Dictionary<string, object?>>();
        var rejected = new List<Dictionary<string, object?>>();
        foreach (var item in items)
        {
            var subject = item.GetProperty("subject_id").GetString()!;
            if (RejectAll || RejectedSubjects.Contains(subject))
            {
                rejected.Add(new Dictionary<string, object?> { ["subject_id"] = subject, ["code"] = RejectionCode, ["detail"] = "rejected by the fake" });
                continue;
            }

            scores.Add(new Dictionary<string, object?>
            {
                ["subject_id"] = subject,
                ["uplift"] = Uplift,
                ["p_treated"] = 0.3m,
                ["p_control"] = 0.1m,
                ["segment"] = DoNotDisturbSubjects.Contains(subject) ? SegmentDoNotDisturb : SegmentPersuadable,
            });
        }

        return Json(HttpStatusCode.OK, new Dictionary<string, object?>
        {
            ["model_name"] = ModelName,
            ["model_version"] = requestedVersion ?? ModelVersion,
            ["feature_schema_version"] = FeatureSchemaVersion,
            ["provenance"] = ProvenanceTrained,
            ["scores"] = scores,
            ["rejected"] = rejected,
        });
    }

    private static Dictionary<string, object?> EmptyModels() => new() { ["models"] = Array.Empty<object>() };

    private static Dictionary<string, object?> Models() => new()
    {
        ["models"] = new[]
        {
            new Dictionary<string, object?>
            {
                ["model_name"] = ModelName,
                ["version"] = ModelVersion,
                ["kind"] = "uplift",
                ["feature_schema_version"] = FeatureSchemaVersion,
                ["provenance"] = ProvenanceTrained,
                ["trained_at"] = "2026-11-01T00:00:00Z",
                ["metrics"] = new Dictionary<string, object?>(),
                ["status"] = "available",
                ["error_code"] = null,
            },
        },
    };

    private static HttpResponseMessage Error(HttpStatusCode status, string code) =>
        Json(status, new Dictionary<string, object?> { ["code"] = code, ["detail"] = "fake refusal", ["trace_id"] = Guid.NewGuid().ToString("D") });

    private static HttpResponseMessage Json(HttpStatusCode status, object body)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return response;
    }
}
