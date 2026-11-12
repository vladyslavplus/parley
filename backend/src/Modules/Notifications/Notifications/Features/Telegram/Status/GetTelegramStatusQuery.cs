using Mediator;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.Status;

/// <summary>Whether the Telegram channel runs here, for any signed-in member.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetTelegramStatusQuery : IRequest<Result<TelegramStatusView>>;
