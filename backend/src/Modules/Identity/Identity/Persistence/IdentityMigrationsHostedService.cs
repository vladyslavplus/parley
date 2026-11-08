using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Parley.Identity.Options.Storage;

namespace Parley.Identity.Persistence;

/// <summary>
/// Applies pending identity migrations at host start when
/// <see cref="IdentityStorageOptions.ApplyMigrationsOnStartup"/> is set and the host
/// runs in the Development environment. Any other environment migrates through an
/// explicit deployment step, so a production start never changes the schema.
/// </summary>
public sealed class IdentityMigrationsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityStorageOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup || !environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
