namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>The dashboard aggregate. Totals only: the view never carries a record or a recomputed statistic.</summary>
/// <param name="Stages">Open deals per pipeline stage, in stage order.</param>
/// <param name="OpenAmount">Sum of the amounts of the open deals in the stages.</param>
/// <param name="OpenDeals">Count of the open deals in the stages.</param>
/// <param name="TasksDue">Open tasks due now or earlier, within the caller's task scope.</param>
/// <param name="LatestCampaign">Latest completed campaign and its verdict; null when the caller may not see campaigns or none completed.</param>
public sealed record DashboardOverviewView(
    IReadOnlyList<DashboardPipelineStageView> Stages,
    decimal OpenAmount,
    int OpenDeals,
    int TasksDue,
    LatestCampaignView? LatestCampaign);
