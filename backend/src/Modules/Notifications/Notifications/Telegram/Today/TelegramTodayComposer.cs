using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Priority;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Link;
using Parley.Notifications.Telegram.Shared;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Telegram.Today;

/// <summary>
/// Answers <c>/today</c> for a chat. The answer honors the scope and permission of the linked membership: a deactivated member
/// gets nothing and loses the link, and a member without the insights permission gets a refusal. The permission is
/// <c>insights.view</c>; the visible records follow the <c>contacts.read</c> scope of the member, as the web list does. When
/// the chat is linked to several organizations, the chat chooses one by its number. The answer names no contact: it lists
/// next actions and links each one to the contact's record in the web app.
/// </summary>
public sealed class TelegramTodayComposer(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext,
    IPermissionChecker permissionChecker,
    IUserScopeReader scopeReader,
    IPriorityReader priorityReader,
    TelegramLinkEnder linkEnder,
    IOptions<TelegramOptions> telegramOptions,
    IOptions<NotificationsOptions> notificationsOptions)
{
    private const string TodayCommand = "/today";

    /// <summary>Returns the reply text. The caller owns the transaction; the tenant is set to the chosen organization.</summary>
    public async Task<string> ComposeAsync(long chatId, string? languageCode, string? argument, CancellationToken cancellationToken)
    {
        var links = await dbContext.TelegramLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(link => link.ChatId == chatId && link.Active)
            .OrderBy(link => link.LinkedAt)
            .ThenBy(link => link.Id)
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return TelegramTexts.Get(TelegramTextKeys.NotLinked, TelegramLocale.Resolve(languageCode));
        }

        if (links.Count == 1)
        {
            return await AnswerForAsync(links[0], cancellationToken);
        }

        if (!TryChoose(argument, links.Count, out var chosen))
        {
            return ChooseText(links.Count, links[0].Locale);
        }

        return await AnswerForAsync(links[chosen], cancellationToken);
    }

    private async Task<string> AnswerForAsync(TelegramLink link, CancellationToken cancellationToken)
    {
        var locale = link.Locale;
        tenantContext.Set(link.OrganizationId);

        if (!await permissionChecker.IsActiveMemberAsync(link.UserId, link.OrganizationId, cancellationToken))
        {
            var tracked = await dbContext.TelegramLinks.FirstAsync(candidate => candidate.Id == link.Id, cancellationToken);
            await linkEnder.EndAsync(tracked, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return TelegramTexts.Get(TelegramTextKeys.MembershipEnded, locale);
        }

        // The insights permission gates the answer; the record scope comes from the contacts permission, exactly like the
        // priority list on the web. Reading the scope of insights.view would give every role the organization-wide list.
        if (!await permissionChecker.HasPermissionAsync(link.UserId, link.OrganizationId, Permissions.InsightsView, cancellationToken))
        {
            return TelegramTexts.Get(TelegramTextKeys.NoAccess, locale);
        }

        var scope = await scopeReader.ReadAsync(link.UserId, link.OrganizationId, Permissions.ContactsRead, cancellationToken);
        if (scope.Level == DataScopeLevel.None)
        {
            return TelegramTexts.Get(TelegramTextKeys.NoAccess, locale);
        }

        var entries = await ReadEntriesAsync(link.UserId, scope, cancellationToken);
        if (entries.Count == 0)
        {
            return TelegramTexts.Get(TelegramTextKeys.TodayEmpty, locale);
        }

        return ListText(entries, locale);
    }

    private async Task<IReadOnlyList<PriorityEntry>> ReadEntriesAsync(Guid userId, DataScope scope, CancellationToken cancellationToken)
    {
        var limit = telegramOptions.Value.TodayItemCount;

        // The reader ranks only what the caller may see: the member's own contacts, the team's contacts, or the whole organization.
        // Ranking the whole organization first and filtering after could leave a team with nothing to show.
        IReadOnlyCollection<Guid>? owners = scope.Level switch
        {
            DataScopeLevel.Organization => null,
            DataScopeLevel.Own => [userId],
            _ => scope.OwnerUserIds?.ToArray(),
        };
        var pool = await priorityReader.ReadTopAsync(owners, limit, cancellationToken);
        // A do-not-disturb caution is not an action: the chat never gets "call this contact" for one. The reader drops
        // such rows too; this keeps the rule in the channel for any reader implementation.
        return TelegramTodayVisibility.SelectVisible(scope, userId, pool, limit)
            .Where(entry => !entry.ReasonCodes.Contains(PriorityReasonCodes.DoNotDisturb))
            .Take(limit)
            .ToArray();
    }

    private string ListText(IReadOnlyList<PriorityEntry> entries, string locale)
    {
        var baseUrl = notificationsOptions.Value.PublicWebBaseUrl;
        var lines = new List<string> { TelegramTexts.Get(TelegramTextKeys.TodayHeader, locale) };
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var action = TelegramTexts.Get(TelegramTextKeys.ActionPrefix + entry.SuggestedAction, locale);

            // The action text stays free of personal data; the link opens the contact's record for the name and history.
            var recordUrl = NotificationWebPaths.BuildUrl(baseUrl, locale, NotificationWebPaths.ContactRecord(entry.ContactId));
            lines.Add($"{(index + 1).ToString(CultureInfo.InvariantCulture)}. {action}\n{recordUrl}");
        }

        var insightsUrl = NotificationWebPaths.BuildUrl(baseUrl, locale, NotificationWebPaths.AppHome);
        lines.Add(TelegramTexts.Format(TelegramTextKeys.InsightsFooter, locale, insightsUrl));
        return string.Join('\n', lines);
    }

    private static string ChooseText(int count, string locale)
    {
        var commands = Enumerable.Range(1, count)
            .Select(number => $"{TodayCommand} {number.ToString(CultureInfo.InvariantCulture)}");
        return TelegramTexts.Format(TelegramTextKeys.ChooseOrganization, locale, count) + "\n" + string.Join('\n', commands);
    }

    private static bool TryChoose(string? argument, int count, out int index)
    {
        index = 0;
        if (!int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1 || number > count)
        {
            return false;
        }

        index = number - 1;
        return true;
    }
}
