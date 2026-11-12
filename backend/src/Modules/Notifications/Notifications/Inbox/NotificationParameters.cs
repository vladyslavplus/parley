using System.Globalization;

namespace Parley.Notifications.Inbox;

/// <summary>
/// Parameter names a notification stores, and the helpers that build them. Values are identifiers and instants in invariant
/// text, never rendered sentences.
/// </summary>
public static class NotificationParameters
{
    public const string TaskId = "taskId";

    public const string DealId = "dealId";

    public const string CampaignId = "campaignId";

    private const string ScoringRunId = "scoringRunId";

    private const string ScoreKind = "scoreKind";

    private const string ModelVersion = "modelVersion";

    public const string UserId = "userId";

    private const string DueAt = "dueAt";

    private const string IdFormat = "D";

    private const string InstantFormat = "O";

    public static Dictionary<string, string> Id(string name, Guid id) => new() { [name] = id.ToString(IdFormat) };

    public static Dictionary<string, string> TaskDue(Guid taskId, DateTime dueAt) => new()
    {
        [TaskId] = taskId.ToString(IdFormat),
        [DueAt] = dueAt.ToString(InstantFormat, CultureInfo.InvariantCulture),
    };

    public static Dictionary<string, string> ScoringRun(Guid scoringRunId, string scoreKind, string modelVersion) => new()
    {
        [ScoringRunId] = scoringRunId.ToString(IdFormat),
        [ScoreKind] = scoreKind,
        [ModelVersion] = modelVersion,
    };
}
