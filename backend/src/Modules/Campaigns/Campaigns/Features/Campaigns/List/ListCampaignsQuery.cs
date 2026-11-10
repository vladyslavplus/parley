using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.List;

/// <summary>Lists the campaigns of the organization, keyset-paged, optionally narrowed by name text and state.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the campaign name must contain.</param>
/// <param name="Status">Only campaigns in this state; null for every state.</param>
/// <param name="Sort">Sort field: <c>name</c> (default) or <c>created</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record ListCampaignsQuery(
    string? After,
    int? Limit,
    string? Q,
    string? Status,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<CampaignSummaryView>>>;
