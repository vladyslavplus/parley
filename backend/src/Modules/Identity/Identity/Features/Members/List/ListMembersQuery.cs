using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Members.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.List;

/// <summary>
/// Lists the members of the current organization, keyset-paged by membership identifier.
/// </summary>
/// <param name="After">Last identifier of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ListMembersQuery(Guid? After, int? Limit) : IRequest<Result<CursorPage<MemberView>>>;
