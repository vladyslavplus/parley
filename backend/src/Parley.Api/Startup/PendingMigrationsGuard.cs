using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Persistence;
using Parley.Crm.Persistence;
using Parley.Identity.Persistence;
using Parley.Insights.Persistence;

namespace Parley.Api.Startup;

/// <summary>
/// Outside the Development environment the modules never migrate at start, so a database that lags the code would fail on
/// the first request. This check runs at host start and stops the host with the names of the modules whose migrations are
/// pending, so the gap is found at deployment and not by a user. In Development the modules apply their migrations and this
/// check is skipped.
/// </summary>
public sealed class PendingMigrationsGuard(IServiceScopeFactory scopeFactory, IHostEnvironment environment) : IHostedService
{
    private static readonly (string Module, Type Context)[] Modules =
    [
        ("identity", typeof(IdentityDbContext)),
        ("crm", typeof(CrmDbContext)),
        ("campaigns", typeof(CampaignsDbContext)),
        ("insights", typeof(InsightsDbContext)),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var pending = new List<string>();
        foreach (var (module, contextType) in Modules)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                pending.Add(module);
            }
        }

        if (pending.Count > 0)
        {
            throw new InvalidOperationException(
                $"Database migrations are pending for: {string.Join(", ", pending)}. Apply them with the deployment migration step "
                + "before starting the API outside the Development environment.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
