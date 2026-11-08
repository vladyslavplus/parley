using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Organizations.Get;

/// <summary>
/// Reads the settings of the organization the caller is a member of. Any other
/// organization identifier is not found, so existence does not leak.
/// </summary>
/// <param name="OrganizationId">Organization identifier; must be the current organization.</param>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetOrganizationQuery(Guid OrganizationId) : IRequest<Result<OrganizationView>>;
