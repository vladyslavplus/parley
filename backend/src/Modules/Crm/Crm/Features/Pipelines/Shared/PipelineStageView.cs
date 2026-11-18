namespace Parley.Crm.Features.Pipelines.Shared;

/// <summary>A stage of a pipeline as the API returns it.</summary>
/// <param name="Id">Stage identifier.</param>
/// <param name="Name">Stage name.</param>
/// <param name="Position">Order of the stage in the pipeline.</param>
/// <param name="Kind">Stage kind: <c>open</c>, <c>won</c> or <c>lost</c>.</param>
/// <param name="ProbabilityBps">Win probability in basis points (10000 = 100 %).</param>
/// <param name="Color">Colour chosen for the stage as a lowercase six-digit hex triplet such as <c>#2453be</c>.</param>
public sealed record PipelineStageView(
    Guid Id,
    string Name,
    int Position,
    string Kind,
    int ProbabilityBps,
    string Color);
