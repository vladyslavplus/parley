namespace Parley.Identity.Features.Organizations.ListMine;

/// <summary>
/// One organization the signed-in user is an active member of.
/// </summary>
/// <param name="Id">Organization identifier.</param>
/// <param name="Name">Organization display name.</param>
/// <param name="RoleCode">Role the user holds in that organization.</param>
/// <param name="IsCurrent">True for the organization the current access token is scoped to.</param>
public sealed record MyOrganizationView(Guid Id, string Name, string RoleCode, bool IsCurrent);
