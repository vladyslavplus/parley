namespace Parley.Crm.Features.Pipelines.Shared;

/// <summary>
/// A pipeline with its stages in position order. <see cref="Version"/> is the row version: clients send it
/// back on rename, and a stale value returns 409.
/// </summary>
/// <param name="Id">Pipeline identifier.</param>
/// <param name="Name">Pipeline name.</param>
/// <param name="IsDefault">Whether this is the organization's default pipeline.</param>
/// <param name="Stages">Stages in position order; the won and lost stages come last.</param>
/// <param name="Version">Row version to send back on rename.</param>
public sealed record PipelineView(
    Guid Id,
    string Name,
    bool IsDefault,
    IReadOnlyList<PipelineStageView> Stages,
    uint Version);
