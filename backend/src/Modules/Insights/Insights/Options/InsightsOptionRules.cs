namespace Parley.Insights.Options;

/// <summary>Startup checks of the insights options. A bad value stops the host before any job runs.</summary>
public static class InsightsOptionRules
{
    /// <summary>
    /// The priority weights name every scored segment and never rise along the documented order (highest weight first),
    /// and the bonuses and the windows are in range.
    /// </summary>
    public static bool PriorityIsValid(InsightsPriorityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TaskDueBonus < 0m || options.StaleDealBonus < 0m)
        {
            return false;
        }

        if (options.StaleDealDays < 1)
        {
            return false;
        }

        if (options.ItemsPerOwner is < 1 or > InsightsPriorityOptions.MaximumItemsPerOwner)
        {
            return false;
        }

        var ordered = InsightsPriorityOptions.ScoredSegmentsInOrder;
        if (!ordered.All(segment => options.Weights.ContainsKey(segment)))
        {
            return false;
        }

        var weights = ordered.Select(segment => options.Weights[segment]).ToList();
        return weights.All(weight => weight >= 0m)
            && weights.Zip(weights.Skip(1)).All(pair => pair.First >= pair.Second);
    }

    /// <summary>The dashboard answer lives between the minimum and maximum cache seconds.</summary>
    public static bool DashboardIsValid(InsightsDashboardOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.CacheSeconds is >= InsightsDashboardOptions.MinimumCacheSeconds and <= InsightsDashboardOptions.MaximumCacheSeconds;
    }

    /// <summary>The job queue must name a queue.</summary>
    public static bool JobsAreValid(InsightsJobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return !string.IsNullOrWhiteSpace(options.QueueName);
    }
}
