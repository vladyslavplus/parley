using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Organizations.ListMine;

/// <summary>
/// Lists the organizations the signed-in user belongs to. Deactivated memberships are left out.
/// </summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record ListMyOrganizationsQuery : IRequest<Result<IReadOnlyList<MyOrganizationView>>>;
