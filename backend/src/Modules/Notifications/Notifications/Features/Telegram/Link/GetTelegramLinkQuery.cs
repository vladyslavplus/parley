using Mediator;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.Link;

/// <summary>Whether the signed-in user has a Telegram chat linked in the current organization.</summary>
[PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
public sealed record GetTelegramLinkQuery() : IRequest<Result<TelegramLinkView>>;
