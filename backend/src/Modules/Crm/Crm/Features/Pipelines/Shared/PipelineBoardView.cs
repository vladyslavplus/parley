namespace Parley.Crm.Features.Pipelines.Shared;

/// <summary>
/// The board of a pipeline: every stage with its visible deal count and amount total. The cards of one stage
/// come from the deal list filtered by that stage, so the board read stays one statement per aggregate.
/// </summary>
/// <param name="PipelineId">Pipeline identifier.</param>
/// <param name="PipelineName">Pipeline name.</param>
/// <param name="Stages">Stages in position order.</param>
public sealed record PipelineBoardView(
    Guid PipelineId,
    string PipelineName,
    IReadOnlyList<PipelineBoardStageView> Stages);
