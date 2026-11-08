using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ResendConfirmation;

/// <summary>
/// Queues a new confirmation email for an unconfirmed account. Unknown and already
/// confirmed addresses get the same success result and no mail, so the response
/// does not reveal which addresses have accounts.
/// </summary>
/// <param name="Email">Address to send the link to.</param>
/// <param name="Culture">Request culture, used as the email language.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record ResendConfirmationCommand(string Email, string Culture) : IRequest<Result>;
