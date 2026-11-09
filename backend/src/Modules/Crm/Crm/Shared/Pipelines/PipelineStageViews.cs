using System.Linq.Expressions;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Pipelines.Shared;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>Projection and mapping of pipeline stages, shared by every read that returns stages.</summary>
public static class PipelineStageViews
{
    public static readonly Expression<Func<PipelineStage, PipelineStageRow>> Projection = stage => new PipelineStageRow(
        stage.Id,
        stage.Name,
        stage.SystemKey,
        stage.Position,
        stage.Kind,
        stage.ProbabilityBps,
        stage.ColorIndex);

    public static PipelineStageView ToView(PipelineStageRow row) => new(
        row.Id,
        PipelineNames.Display(row.Name, row.SystemKey),
        row.Position,
        row.Kind,
        row.ProbabilityBps,
        row.ColorIndex);
}
