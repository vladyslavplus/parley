using Mediator;
using Parley.Notifications.Telegram.Bot;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Notifications.Features.Telegram.Updates;

/// <summary>
/// One update of the bot, from polling or from the webhook. It has no caller: the chat is the caller, and the tenant is found from
/// the link code or the chat's links. Handling is idempotent by update id.
/// </summary>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record HandleTelegramUpdateCommand(TelegramIncomingUpdate Update) : IRequest<Result<TelegramUpdateResult>>;
