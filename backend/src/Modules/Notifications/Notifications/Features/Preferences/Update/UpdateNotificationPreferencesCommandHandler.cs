using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Preferences;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Preferences.Update;

public sealed class UpdateNotificationPreferencesCommandHandler(
    NotificationsDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesView>>
{
    public async ValueTask<Result<NotificationPreferencesView>> Handle(UpdateNotificationPreferencesCommand command, CancellationToken cancellationToken)
    {
        if (command.Changes.Any(change => !NotificationTypes.All.Contains(change.Type)))
        {
            return Result<NotificationPreferencesView>.Failure(Error.Validation(NotificationErrorCodes.PreferenceTypeUnknown));
        }

        if (command.Changes.Any(change => !NotificationChannels.All.Contains(change.Channel)))
        {
            return Result<NotificationPreferencesView>.Failure(Error.Validation(NotificationErrorCodes.PreferenceChannelUnknown));
        }

        // A type named twice on one channel takes its last switch.
        var switches = command.Changes
            .GroupBy(change => (change.Type, change.Channel))
            .ToDictionary(group => group.Key, group => group.Last().Enabled);

        var userId = currentUser.RequireUserId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await NotificationPreferenceReader.LockAsync(dbContext, userId, cancellationToken);

        var current = await NotificationPreferenceReader.ReadVersionAsync(dbContext, userId, cancellationToken);
        if (current != command.Version)
        {
            return Result<NotificationPreferencesView>.Failure(Error.Conflict(NotificationErrorCodes.PreferencesVersionConflict));
        }

        var stored = await dbContext.Preferences
            .Where(preference => preference.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var ((type, channel), enabled) in switches)
        {
            var row = stored.FirstOrDefault(preference => preference.Type == type && preference.Channel == channel);
            if (row is null)
            {
                dbContext.Preferences.Add(new NotificationPreference
                {
                    OrganizationId = tenantContext.OrganizationId,
                    UserId = userId,
                    Type = type,
                    Channel = channel,
                    Enabled = enabled,
                });
            }
            else
            {
                row.Enabled = enabled;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var view = await NotificationPreferenceReader.ReadAsync(dbContext, userId, cancellationToken);
        return Result<NotificationPreferencesView>.Success(view);
    }
}
