namespace Parley.Crm.Contracts.Dashboard;

/// <summary>Open deals of one pipeline stage: how many, and their summed amount in the organization currency.</summary>
/// <param name="StageId">The stage.</param>
/// <param name="StageName">Display name of the stage.</param>
/// <param name="Position">Order of the stage in its pipeline; stages of several pipelines sort by position, then name.</param>
/// <param name="DealCount">Open deals in the stage.</param>
/// <param name="Amount">Sum of their amounts.</param>
public sealed record CrmPipelineStageTotal(Guid StageId, string StageName, int Position, int DealCount, decimal Amount);
