namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>One stage of the open pipeline.</summary>
/// <param name="StageId">The stage.</param>
/// <param name="StageName">Display name of the stage.</param>
/// <param name="Position">Order of the stage.</param>
/// <param name="DealCount">Open deals in the stage.</param>
/// <param name="Amount">Sum of their amounts.</param>
public sealed record DashboardPipelineStageView(Guid StageId, string StageName, int Position, int DealCount, decimal Amount);
