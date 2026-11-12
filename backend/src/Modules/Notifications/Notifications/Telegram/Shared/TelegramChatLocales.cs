using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Persistence;

namespace Parley.Notifications.Telegram.Shared;

/// <summary>
/// Picks the culture of a chat's messages: the locale of its active link when there is one, otherwise the chat's own language.
/// The chat is not tenant-scoped here, because a chat can belong to several organizations.
/// </summary>
public sealed class TelegramChatLocales(NotificationsDbContext dbContext)
{
    public async Task<string> ResolveAsync(long chatId, string? languageCode, CancellationToken cancellationToken)
    {
        var locale = await dbContext.TelegramLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(link => link.ChatId == chatId && link.Active)
            .OrderBy(link => link.LinkedAt)
            .ThenBy(link => link.Id)
            .Select(link => link.Locale)
            .FirstOrDefaultAsync(cancellationToken);

        return locale ?? TelegramLocale.Resolve(languageCode);
    }
}
