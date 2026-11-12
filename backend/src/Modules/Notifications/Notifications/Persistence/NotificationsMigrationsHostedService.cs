using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Parley.Notifications.Options;

namespace Parley.Notifications.Persistence;

/// <summary>
/// Applies pending notifications migrations at host start when <see cref="NotificationsStorageOptions.ApplyMigrationsOnStartup"/>
/// is set and the host runs in the Development environment. Any other environment migrates through an explicit deployment step.
/// </summary>
public sealed class NotificationsMigrationsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationsStorageOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup || !environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
