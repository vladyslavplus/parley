using Mediator;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Preferences.Get;

public sealed class GetNotificationPreferencesQueryHandler(NotificationsDbContext dbContext, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesView>>
{
    public async ValueTask<Result<NotificationPreferencesView>> Handle(GetNotificationPreferencesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var view = await NotificationPreferenceReader.ReadAsync(dbContext, userId, cancellationToken);
        return Result<NotificationPreferencesView>.Success(view);
    }
}
