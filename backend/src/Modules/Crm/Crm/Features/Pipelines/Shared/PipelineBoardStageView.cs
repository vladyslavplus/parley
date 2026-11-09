namespace Parley.Crm.Features.Pipelines.Shared;

/// <summary>One column of the board.</summary>
/// <param name="Id">Stage identifier.</param>
/// <param name="Name">Stage name.</param>
/// <param name="Position">Order of the stage in the pipeline.</param>
/// <param name="Kind">Stage kind: <c>open</c>, <c>won</c> or <c>lost</c>.</param>
/// <param name="ProbabilityBps">Win probability in basis points.</param>
/// <param name="ColorIndex">Index into the color ramp, 0 to 4.</param>
/// <param name="DealCount">Visible deals on the stage.</param>
/// <param name="TotalAmount">Sum of the amounts of the visible deals on the stage.</param>
public sealed record PipelineBoardStageView(
    Guid Id,
    string Name,
    int Position,
    string Kind,
    int ProbabilityBps,
    int ColorIndex,
    int DealCount,
    decimal TotalAmount);
