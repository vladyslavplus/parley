using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Persistence;
using Parley.Crm.Persistence;
using Parley.Identity.Persistence;
using Parley.Insights.Persistence;
using Parley.Notifications.Persistence;

namespace Parley.Api.Startup;

/// <summary>
/// The one-shot migration step of a deployment: <c>dotnet Parley.Api.dll --migrate</c> applies every module's migrations and
/// exits. Outside the Development environment the modules never migrate on their own, so the compose <c>migrate</c> service
/// runs this before the API starts. Running it against an up-to-date database logs and changes nothing.
/// </summary>
public static class MigrationCommand
{
    /// <summary>Command-line switch that runs the migrations instead of the web host.</summary>
    public const string Argument = "--migrate";

    private static readonly (string Module, Type Context)[] Modules =
    [
        ("identity", typeof(IdentityDbContext)),
        ("crm", typeof(CrmDbContext)),
        ("campaigns", typeof(CampaignsDbContext)),
        ("insights", typeof(InsightsDbContext)),
        ("notifications", typeof(NotificationsDbContext)),
    ];

    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MigrationCommand));
        await using var scope = services.CreateAsyncScope();
        foreach (var (module, contextType) in Modules)
        {
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var count = pending.Count();
            if (count == 0)
            {
                logger.LogInformation("Migrations: {Module} is up to date.", module);
                continue;
            }

            logger.LogInformation("Migrations: applying {Count} to {Module}.", count, module);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        logger.LogInformation("Migrations: every schema is up to date.");
        return 0;
    }
}
