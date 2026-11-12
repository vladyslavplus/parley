using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Link;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Telegram.Link;

public sealed class UnlinkTelegramCommandHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    TelegramLinkEnder linkEnder,
    IOptions<TelegramOptions> options) : IRequestHandler<UnlinkTelegramCommand, Result>
{
    public async ValueTask<Result> Handle(UnlinkTelegramCommand command, CancellationToken cancellationToken)
    {
        if (!TelegramOptionRules.IsConfigured(options.Value))
        {
            return Result.Failure(Error.NotFound(TelegramErrorCodes.NotConfigured));
        }

        var userId = currentUser.RequireUserId();
        var link = await dbContext.TelegramLinks
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId && candidate.Active, cancellationToken);
        if (link is null)
        {
            return Result.Failure(Error.NotFound(TelegramErrorCodes.LinkNotFound));
        }

        await linkEnder.EndAsync(link, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
