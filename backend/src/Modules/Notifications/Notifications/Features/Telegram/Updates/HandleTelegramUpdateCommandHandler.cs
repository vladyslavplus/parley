using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Domain.Telegram.Commands;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Bot;
using Parley.Notifications.Telegram.Link;
using Parley.Notifications.Telegram.Shared;
using Parley.Notifications.Telegram.Today;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Telegram.Updates;

/// <summary>
/// Records the update id and handles the command in one transaction. A replayed id fails the first insert and does nothing.
/// The reply is returned, not sent: the dispatcher sends it after the commit, so no network call happens inside a transaction.
/// </summary>
public sealed class HandleTelegramUpdateCommandHandler(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext,
    TelegramLinkRedeemer redeemer,
    TelegramChatUnlinker unlinker,
    TelegramTodayComposer today,
    TelegramChatLocales chatLocales,
    TimeProvider timeProvider)
    : IRequestHandler<HandleTelegramUpdateCommand, Result<TelegramUpdateResult>>
{
    public async ValueTask<Result<TelegramUpdateResult>> Handle(HandleTelegramUpdateCommand command, CancellationToken cancellationToken)
    {
        var update = command.Update;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.TelegramProcessedUpdates.Add(new TelegramProcessedUpdate
        {
            UpdateId = update.UpdateId,
            ReceivedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Result<TelegramUpdateResult>.Success(new TelegramUpdateResult(null));
        }

        if (update.ChatId is not { } chatId)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result<TelegramUpdateResult>.Success(new TelegramUpdateResult(null));
        }

        var (text, organizationId) = await ReplyAsync(chatId, update, cancellationToken);

        // A tenant set inside an awaited call does not flow back here, so a redeemed organization is set again for the save.
        if (organizationId is { } redeemed)
        {
            tenantContext.Set(redeemed);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<TelegramUpdateResult>.Success(new TelegramUpdateResult(new TelegramReply(chatId, text)));
    }

    private async Task<(string Text, Guid? OrganizationId)> ReplyAsync(long chatId, TelegramIncomingUpdate update, CancellationToken cancellationToken)
    {
        var command = TelegramCommandParser.Parse(update.Text);
        if (command.Kind == TelegramCommandKind.Start && command.Argument is not null)
        {
            var redemption = await redeemer.RedeemAsync(chatId, update.LanguageCode, command.Argument, cancellationToken);
            return (redemption.Text, redemption.OrganizationId);
        }

        var text = command.Kind switch
        {
            TelegramCommandKind.Unlink =>
                await unlinker.UnlinkChatAsync(chatId, update.LanguageCode, cancellationToken),
            TelegramCommandKind.Today =>
                await today.ComposeAsync(chatId, update.LanguageCode, command.Argument, cancellationToken),
            _ => TelegramTexts.Get(
                TelegramTextKeys.Help,
                await chatLocales.ResolveAsync(chatId, update.LanguageCode, cancellationToken)),
        };
        return (text, null);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
