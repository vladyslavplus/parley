using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Pipelines.List;

public sealed class ListPipelinesQueryHandler(CrmDbContext dbContext, DefaultPipelineProvisioner provisioner)
    : IRequestHandler<ListPipelinesQuery, Result<KeysetCursorPage<PipelineSummaryView>>>
{
    public async ValueTask<Result<KeysetCursorPage<PipelineSummaryView>>> Handle(ListPipelinesQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var position = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, PipelineSortFields.Name, KeysetOrdering.ParseText);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        await provisioner.EnsureSavedAsync(cancellationToken);

        var pipelines = dbContext.Pipelines.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search);
            pipelines = pipelines.Where(pipeline => EF.Functions.ILike(pipeline.Name, pattern));
        }

        var ordered = KeysetOrdering.Apply(pipelines, pipeline => pipeline.Name, pipeline => pipeline.Id, position, descending);
        var rows = await ordered
            .Select(pipeline => new PipelineSummaryRow(
                pipeline.Id,
                pipeline.Name,
                pipeline.SystemKey,
                pipeline.IsDefault,
                pipeline.Stages.Count(),
                EF.Property<uint>(pipeline, PersistenceConstants.XminPropertyName)))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var page = KeysetCursorPage<PipelineSummaryRow>.From(
            rows,
            limit,
            row => KeysetOrdering.CursorFor(PipelineSortFields.Name, row.Name, row.Id));
        var items = page.Items
            .Select(row => new PipelineSummaryView(row.Id, PipelineNames.Display(row.Name, row.SystemKey), row.IsDefault, row.StageCount, row.Version))
            .ToArray();
        return Result<KeysetCursorPage<PipelineSummaryView>>.Success(new KeysetCursorPage<PipelineSummaryView>(items, page.NextCursor));
    }

    private static Result<KeysetCursorPage<PipelineSummaryView>> Failure() =>
        Result<KeysetCursorPage<PipelineSummaryView>>.Failure(CrmErrors.Field(nameof(ListPipelinesQuery.After), ErrorCodes.ValidationFailed));
}
