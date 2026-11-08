using Mediator;
using Parley.Identity.Authentication.Tokens;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Organizations.Switch;

/// <summary>
/// Switches the session to another organization the caller is an active member of, and
/// issues a new token pair for it. The new access token carries the target organization.
/// Any other organization is not found, so membership elsewhere does not leak. The current
/// session is ended: its refresh token must be active and belong to the caller, and the
/// session family is revoked before the new pair is issued, so the old session stops working.
/// </summary>
/// <param name="OrganizationId">Target organization.</param>
/// <param name="RefreshToken">Active refresh token of the current session.</param>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record SwitchOrganizationCommand(Guid OrganizationId, string RefreshToken) : IRequest<Result<AuthTokens>>;
