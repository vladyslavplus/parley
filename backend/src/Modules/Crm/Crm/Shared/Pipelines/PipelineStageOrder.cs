using Microsoft.EntityFrameworkCore;
using Parley.Crm.Persistence;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>
/// Writes the positions of a pipeline's stages in one order. The unique position index never sees two
/// stages on one value: every stage first moves to a temporary negative position, then to its final one.
/// Runs inside the caller's transaction.
/// </summary>
public static class PipelineStageOrder
{
    public static async Task WriteAsync(CrmDbContext dbContext, IReadOnlyList<Guid> orderedStageIds, CancellationToken cancellationToken)
    {
        for (var index = 0; index < orderedStageIds.Count; index++)
        {
            var stageId = orderedStageIds[index];
            var temporary = -(index + 1);
            await dbContext.PipelineStages
                .Where(stage => stage.Id == stageId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(stage => stage.Position, temporary), cancellationToken);
        }

        for (var index = 0; index < orderedStageIds.Count; index++)
        {
            var stageId = orderedStageIds[index];
            var final = index;
            await dbContext.PipelineStages
                .Where(stage => stage.Id == stageId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(stage => stage.Position, final), cancellationToken);
        }
    }
}
