namespace Parley.Crm.Features.Pipelines.Shared;

/// <summary>A pipeline in a list, without its stages.</summary>
/// <param name="Id">Pipeline identifier.</param>
/// <param name="Name">Pipeline name.</param>
/// <param name="IsDefault">Whether this is the organization's default pipeline.</param>
/// <param name="StageCount">Number of stages.</param>
/// <param name="Version">Row version.</param>
public sealed record PipelineSummaryView(
    Guid Id,
    string Name,
    bool IsDefault,
    int StageCount,
    uint Version);
