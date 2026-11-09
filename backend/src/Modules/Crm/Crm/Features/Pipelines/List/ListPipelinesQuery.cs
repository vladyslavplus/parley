using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.List;

/// <summary>Lists the pipelines of the organization, keyset-paged. Creates the default pipeline of an organization that has none.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the name must contain.</param>
/// <param name="Sort">Sort field: <c>name</c> (default).</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record ListPipelinesQuery(
    string? After,
    int? Limit,
    string? Search,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<PipelineSummaryView>>>;
