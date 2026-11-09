using Mediator;
using Parley.Crm.Features.Tags.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.List;

/// <summary>Lists the tags of the organization by name, keyset-paged, optionally narrowed by a text.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the tag name must contain.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record ListTagsQuery(string? After, int? Limit, string? Q) : IRequest<Result<KeysetCursorPage<TagView>>>;
