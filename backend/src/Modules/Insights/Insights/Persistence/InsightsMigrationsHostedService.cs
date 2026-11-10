using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Parley.Insights.Options;

namespace Parley.Insights.Persistence;

/// <summary>
/// Applies pending insights migrations at host start when <see cref="InsightsStorageOptions.ApplyMigrationsOnStartup"/> is
/// set and the host runs in the Development environment. Any other environment migrates through an explicit deployment
/// step, so a production start never changes the schema.
/// </summary>
public sealed class InsightsMigrationsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<InsightsStorageOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup || !environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
