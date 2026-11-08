using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Invitations.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.Invite;

/// <summary>
/// Creates an invitation for an address to join the current organization with a system
/// role and an optional team. The token is not created here: the delivery job creates it
/// after this transaction commits, so the token never sits in job storage.
/// </summary>
/// <param name="Email">Address to invite.</param>
/// <param name="RoleCode">Code of the system role granted on acceptance.</param>
/// <param name="TeamId">Team of the new member, or null for none.</param>
/// <param name="Culture">Request culture, used as the email language.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record InviteMemberCommand(string Email, string RoleCode, Guid? TeamId, string Culture) : IRequest<Result<InvitationView>>;
