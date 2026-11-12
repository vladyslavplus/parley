using Mediator;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.LinkCode;

/// <summary>Creates a one-time code that links the signed-in user's Telegram chat to the current organization.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record CreateTelegramLinkCodeCommand() : IRequest<Result<TelegramLinkCodeView>>;
