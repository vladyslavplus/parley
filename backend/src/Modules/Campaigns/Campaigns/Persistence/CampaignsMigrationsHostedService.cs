using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Persistence;

/// <summary>
/// Applies pending campaigns migrations at host start when <see cref="CampaignsStorageOptions.ApplyMigrationsOnStartup"/>
/// is set and the host runs in the Development environment. Any other environment migrates through an explicit
/// deployment step, so a production start never changes the schema.
/// </summary>
public sealed class CampaignsMigrationsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CampaignsStorageOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup || !environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
