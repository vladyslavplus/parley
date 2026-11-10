using Mediator;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.List;

/// <summary>Lists live templates by name, keyset-paged, optionally narrowed by a text.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the template name must contain.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record ListTemplatesQuery(string? After, int? Limit, string? Q) : IRequest<Result<KeysetCursorPage<TemplateSummaryView>>>;
