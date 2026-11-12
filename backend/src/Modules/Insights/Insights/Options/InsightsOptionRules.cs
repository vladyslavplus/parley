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

        if (options.UpliftWeight < 0m || options.RfmWeight < 0m || options.UpliftWeight + options.RfmWeight <= 0m)
        {
            return false;
        }

        if (options.UpliftHighThreshold is < 0m or > 1m)
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

    /// <summary>
    /// The scoring options name an absolute http or https base address, and the timeouts, the batch size, the rejected share
    /// and the horizon are in range. The total timeout is never shorter than one attempt. The API key is not checked here:
    /// a missing key makes the client make no call, which fails the run with a stable code instead of stopping the host.
    /// </summary>
    public static bool ScoringIsValid(InsightsScoringOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (options.AttemptTimeoutSeconds is < 1 or > 60)
        {
            return false;
        }

        if (options.TotalTimeoutSeconds is < 1 or > 120 || options.TotalTimeoutSeconds < options.AttemptTimeoutSeconds)
        {
            return false;
        }

        if (options.BatchSize is < 1 or > InsightsScoringOptions.MaximumBatchSize)
        {
            return false;
        }

        if (options.MaximumRejectedShare is < 0m or > 1m)
        {
            return false;
        }

        if (options.ClvHorizonDays is < 1 or > InsightsScoringOptions.MaximumHorizonDays)
        {
            return false;
        }

        if (options.RunStaleAfter < TimeSpan.FromMinutes(1) || options.RunStaleAfter > TimeSpan.FromDays(7))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(options.QueueName);
    }

    /// <summary>The job queue must name a queue.</summary>
    public static bool JobsAreValid(InsightsJobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return !string.IsNullOrWhiteSpace(options.QueueName);
    }
}
