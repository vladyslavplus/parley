using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Telegram.LinkCode;

/// <summary>
/// Makes the code and stores only its hash. A user who already has an active link gets a conflict: a new link needs the old one
/// ended first. The channel must have a token; without one the answer is <c>telegram.not_configured</c>.
/// </summary>
public sealed class CreateTelegramLinkCodeCommandHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    TelegramLinkCodeLimiter limiter,
    IOptions<TelegramOptions> options,
    TimeProvider timeProvider) : IRequestHandler<CreateTelegramLinkCodeCommand, Result<TelegramLinkCodeView>>
{
    public async ValueTask<Result<TelegramLinkCodeView>> Handle(CreateTelegramLinkCodeCommand command, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!TelegramOptionRules.IsConfigured(settings))
        {
            return Result<TelegramLinkCodeView>.Failure(Error.NotFound(TelegramErrorCodes.NotConfigured));
        }

        var userId = currentUser.RequireUserId();
        if (await dbContext.TelegramLinks.AnyAsync(link => link.UserId == userId && link.Active, cancellationToken))
        {
            return Result<TelegramLinkCodeView>.Failure(Error.Conflict(TelegramErrorCodes.LinkAlreadyExists));
        }

        if (!limiter.TryAcquire(userId))
        {
            return Result<TelegramLinkCodeView>.Failure(Error.RateLimited(TelegramErrorCodes.LinkCodeRateLimited));
        }

        var code = TelegramLinkRules.NewCode();
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime + settings.LinkCodeTtl;
        dbContext.TelegramLinkCodes.Add(new TelegramLinkCode
        {
            OrganizationId = tenantContext.OrganizationId,
            UserId = userId,
            CodeHash = TelegramLinkRules.HashCode(code),
            ExpiresAt = expiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var view = new TelegramLinkCodeView(code, TelegramLinkRules.DeepLink(settings.BotUsername, code), expiresAt);
        return Result<TelegramLinkCodeView>.Success(view);
    }
}
