namespace Parley.Crm.Shared.Pipelines;

/// <summary>Stage columns read in one statement. The name is shown through <see cref="PipelineNames"/> after the read.</summary>
public sealed record PipelineStageRow(
    Guid Id,
    string Name,
    string? SystemKey,
    int Position,
    string Kind,
    int ProbabilityBps,
    int ColorIndex);
