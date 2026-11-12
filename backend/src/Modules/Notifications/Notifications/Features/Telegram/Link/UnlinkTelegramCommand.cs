using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.Link;

/// <summary>Ends the signed-in user's Telegram link in the current organization. Pending messages to that chat are cancelled.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record UnlinkTelegramCommand() : IRequest<Result>;
