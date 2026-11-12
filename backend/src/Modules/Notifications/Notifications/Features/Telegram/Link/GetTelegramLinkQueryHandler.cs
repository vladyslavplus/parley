using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Telegram.Link;

public sealed class GetTelegramLinkQueryHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<TelegramOptions> options) : IRequestHandler<GetTelegramLinkQuery, Result<TelegramLinkView>>
{
    public async ValueTask<Result<TelegramLinkView>> Handle(GetTelegramLinkQuery query, CancellationToken cancellationToken)
    {
        if (!TelegramOptionRules.IsConfigured(options.Value))
        {
            return Result<TelegramLinkView>.Failure(Error.NotFound(TelegramErrorCodes.NotConfigured));
        }

        var userId = currentUser.RequireUserId();
        var linkedAt = await dbContext.TelegramLinks
            .AsNoTracking()
            .Where(link => link.UserId == userId && link.Active)
            .Select(link => (DateTime?)link.LinkedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<TelegramLinkView>.Success(new TelegramLinkView(linkedAt is not null, linkedAt));
    }
}
