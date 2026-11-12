using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Authorization;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Contracts.Events;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Shared;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Telegram.Link;

/// <summary>
/// Redeems a link code sent with <c>/start</c>. The code finds its organization across tenants, because the chat does not
/// know one. The tenant is then set to that organization, and the membership is checked. The code is spent with a conditional
/// update, so two redemptions of one code cannot both link. The caller owns the transaction and the save.
/// </summary>
public sealed class TelegramLinkRedeemer(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext,
    IPermissionChecker permissionChecker,
    IModuleOutbox<NotificationsDbContext> outbox,
    TimeProvider timeProvider)
{
    public async Task<TelegramRedemption> RedeemAsync(long chatId, string? languageCode, string? code, CancellationToken cancellationToken)
    {
        var locale = TelegramLocale.Resolve(languageCode);
        if (!TelegramLinkRules.HasValidShape(code))
        {
            return Refused(TelegramErrorCodes.LinkCodeInvalid, locale);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = TelegramLinkRules.HashCode(code!);
        var linkCode = await dbContext.TelegramLinkCodes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.CodeHash == hash, cancellationToken);
        if (linkCode is null || !TelegramLinkRules.IsRedeemable(linkCode.ExpiresAt, linkCode.UsedAt, now))
        {
            return Refused(TelegramErrorCodes.LinkCodeInvalid, locale);
        }

        var organizationId = linkCode.OrganizationId;
        tenantContext.Set(organizationId);
        var userId = linkCode.UserId;
        if (!await permissionChecker.IsActiveMemberAsync(userId, organizationId, cancellationToken))
        {
            return new TelegramRedemption(TelegramTexts.Get(TelegramErrorCodes.LinkCodeInvalid, locale), organizationId);
        }

        if (await dbContext.TelegramLinks.AnyAsync(link => link.ChatId == chatId && link.Active, cancellationToken))
        {
            return new TelegramRedemption(TelegramTexts.Get(TelegramErrorCodes.ChatAlreadyLinked, locale), organizationId);
        }

        if (await dbContext.TelegramLinks.AnyAsync(link => link.UserId == userId && link.Active, cancellationToken))
        {
            return new TelegramRedemption(TelegramTexts.Get(TelegramErrorCodes.LinkAlreadyExists, locale), organizationId);
        }

        var spent = await dbContext.TelegramLinkCodes
            .IgnoreQueryFilters()
            .Where(candidate => candidate.Id == linkCode.Id && candidate.UsedAt == null && candidate.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.UsedAt, now), cancellationToken);
        if (spent == 0)
        {
            return new TelegramRedemption(TelegramTexts.Get(TelegramErrorCodes.LinkCodeInvalid, locale), organizationId);
        }

        dbContext.TelegramLinks.Add(new TelegramLink
        {
            OrganizationId = organizationId,
            UserId = userId,
            ChatId = chatId,
            Locale = locale,
            Active = true,
            LinkedAt = now,
        });
        outbox.Add(new TelegramLinked { OrganizationId = organizationId, UserId = userId });
        return new TelegramRedemption(TelegramTexts.Get(TelegramTextKeys.Linked, locale), organizationId);
    }

    private static TelegramRedemption Refused(string code, string locale) =>
        new(TelegramTexts.Get(code, locale), null);
}
