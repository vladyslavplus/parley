using Microsoft.EntityFrameworkCore;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>Reads pipelines with their stages. Two statements: the header and the stages.</summary>
public static class PipelineReader
{
    public static async Task<PipelineView?> ReadAsync(CrmDbContext dbContext, Guid pipelineId, CancellationToken cancellationToken)
    {
        var header = await dbContext.Pipelines.AsNoTracking()
            .Where(pipeline => pipeline.Id == pipelineId)
            .Select(pipeline => new PipelineHeader(
                pipeline.Id,
                pipeline.Name,
                pipeline.SystemKey,
                pipeline.IsDefault,
                EF.Property<uint>(pipeline, PersistenceConstants.XminPropertyName)))
            .FirstOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return null;
        }

        var stages = await dbContext.PipelineStages.AsNoTracking()
            .Where(stage => stage.PipelineId == pipelineId)
            .OrderBy(stage => stage.Position)
            .Select(PipelineStageViews.Projection)
            .ToListAsync(cancellationToken);
        return new PipelineView(
            header.Id,
            PipelineNames.Display(header.Name, header.SystemKey),
            header.IsDefault,
            stages.Select(PipelineStageViews.ToView).ToList(),
            header.Version);
    }
}
