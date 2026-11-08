using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.Register;

/// <summary>
/// Creates a user and the organization the user administers. The account stays
/// unconfirmed until the emailed link is used. Both rows commit in one transaction;
/// the confirmation email is queued only after that commit.
/// </summary>
/// <param name="Email">Sign-in address; also the user name.</param>
/// <param name="Password">Plain password, hashed before storage, never logged.</param>
/// <param name="OrganizationName">Name of the organization created for the user.</param>
/// <param name="Culture">Request culture, used as the account locale and the email language.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record RegisterAccountCommand(
    string Email,
    string Password,
    string OrganizationName,
    string Culture) : IRequest<Result>;
