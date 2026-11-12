using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Recipients.Unsubscribe;

/// <summary>
/// Suppresses the address of a campaign recipient, using the signed token from the email link. The token is the
/// credential, so the request is anonymous. Repeating the request changes nothing.
/// </summary>
/// <param name="Token">Unsubscribe token from the link.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record UnsubscribeCommand(string? Token) : IRequest<Result>;
