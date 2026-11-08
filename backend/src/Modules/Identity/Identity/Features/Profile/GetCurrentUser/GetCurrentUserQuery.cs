using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Profile.GetCurrentUser;

/// <summary>Returns the signed-in user, the active organization, role, permissions and scope.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserView>>;
