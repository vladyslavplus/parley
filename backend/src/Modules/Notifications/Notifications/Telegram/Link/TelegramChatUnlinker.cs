using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Shared;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Telegram.Link;

/// <summary>
/// Ends every active link of a chat, across its organizations, for <c>/unlink</c> and for a chat that blocked the bot. Each
/// organization is saved with its own tenant set. The caller owns the transaction.
/// </summary>
public sealed class TelegramChatUnlinker(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext,
    TelegramLinkEnder linkEnder,
    TelegramChatLocales chatLocales)
{
    /// <summary>Ends the chat's links and returns the reply text, in the locale the chat had.</summary>
    public async Task<string> UnlinkChatAsync(long chatId, string? languageCode, CancellationToken cancellationToken)
    {
        var locale = await chatLocales.ResolveAsync(chatId, languageCode, cancellationToken);
        var ended = await EndChatLinksAsync(chatId, cancellationToken);
        return TelegramTexts.Get(ended ? TelegramTextKeys.Unlinked : TelegramTextKeys.NotLinked, locale);
    }

    /// <summary>Ends every active link of the chat. True when at least one link was active.</summary>
    public async Task<bool> EndChatLinksAsync(long chatId, CancellationToken cancellationToken)
    {
        var links = await dbContext.TelegramLinks
            .IgnoreQueryFilters()
            .Where(link => link.ChatId == chatId && link.Active)
            .ToListAsync(cancellationToken);

        foreach (var organization in links.GroupBy(link => link.OrganizationId))
        {
            tenantContext.Set(organization.Key);
            foreach (var link in organization)
            {
                await linkEnder.EndAsync(link, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return links.Count > 0;
    }
}
