using System.Diagnostics;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.DemoSeed;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.DemoSeed;
using Parley.Insights.Contracts.DemoSeed;
using Parley.Notifications.Contracts.DemoSeed;

namespace Parley.Api.Seeding.Demo;

/// <summary>
/// Orchestrates the Development demo seed: environment check, refresh decision, optional wipe, then the identity, CRM,
/// campaign, notification and insights steps in order. The marker is stamped only after every required step succeeds, so
/// a failed start is retried; the insights/scoring step is best effort and never blocks the marker.
/// </summary>
public sealed class DemoSeedRunner(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IOptions<DemoSeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DemoSeedRunner> logger)
{
    public async Task<DemoSeedOutcome> RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var settings = options.Value;

        // The environment is the hard guard: the committed switch is off, and a production host refuses even when it is on.
        if (!DemoSeedPolicy.MayRun(environment))
        {
            logger.LogInformation("Demo seed skipped: the host environment is {Environment}, not Development.", environment.EnvironmentName);
            return new DemoSeedOutcome(DemoSeedOutcome.NotDevelopment, stopwatch.Elapsed);
        }

        if (!settings.Enabled)
        {
            logger.LogInformation("Demo seed skipped: Demo:Seed:Enabled is false.");
            return new DemoSeedOutcome(DemoSeedOutcome.Disabled, stopwatch.Elapsed);
        }

        if (string.IsNullOrWhiteSpace(settings.AccountPassword))
        {
            logger.LogError("Demo seed skipped: Demo:Seed:AccountPassword is not set. Set it in the local Development settings.");
            return new DemoSeedOutcome(DemoSeedOutcome.MissingPassword, stopwatch.Elapsed);
        }

        using var scope = scopeFactory.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityDemoSeeder>();
        var crm = scope.ServiceProvider.GetRequiredService<ICrmDemoSeeder>();
        var campaigns = scope.ServiceProvider.GetRequiredService<ICampaignDemoSeeder>();
        var notifications = scope.ServiceProvider.GetRequiredService<IDemoNotificationSeeder>();
        var insights = scope.ServiceProvider.GetRequiredService<IDemoInsightsSeeder>();

        var organizationId = DemoSeedOrganization.Id;
        var now = timeProvider.GetUtcNow();
        var lastSeeded = await identity.ReadSeededAtAsync(organizationId, cancellationToken);
        if (DemoSeedPolicy.IsFresh(lastSeeded, now.UtcDateTime, settings.RefreshAfterDays))
        {
            logger.LogInformation(
                "Demo seed skipped: the workspace was generated at {SeededAt:O} and the refresh window is {Days} days.",
                lastSeeded,
                settings.RefreshAfterDays);
            return new DemoSeedOutcome(DemoSeedOutcome.Fresh, stopwatch.Elapsed);
        }

        var regenerating = lastSeeded is not null;
        logger.LogInformation(
            "Demo seed starting on {Environment}: organization {OrganizationId}, {Status}, contacts {Contacts}.",
            environment.EnvironmentName,
            organizationId,
            regenerating ? "regenerating a stale workspace" : "seeding a fresh workspace",
            settings.Profile.Contacts);
        try
        {
            if (regenerating)
            {
                logger.LogInformation("Demo workspace is stale; wiping the demo organization data before regeneration.");
                await campaigns.WipeAsync(organizationId, cancellationToken);
                await notifications.WipeAsync(organizationId, cancellationToken);
                await insights.WipeAsync(organizationId, cancellationToken);
                await crm.WipeAsync(organizationId, cancellationToken);
            }

            logger.LogInformation("Demo seed step: identity accounts and teams.");
            var identityResult = await identity.EnsureAsync(DemoSeedOrganization.Profile, settings.AccountPassword, cancellationToken);
            logger.LogInformation(
                "Demo identity ready: {Users} users, {Memberships} memberships, {Teams} teams.",
                identityResult.UsersCreated,
                identityResult.MembershipsCreated,
                identityResult.TeamsCreated);

            var owners = identityResult.Accounts
                .Where(account => account.RoleCode == SystemRoles.SalesRep)
                .Select(account => account.UserId)
                .ToArray();
            logger.LogInformation("Demo seed step: CRM contacts and orders.");
            var crmResult = await crm.SeedAsync(
                new CrmDemoImportRequest(
                    organizationId,
                    DemoSeedOrganization.Currency,
                    DemoSeedOrganization.Locale,
                    owners,
                    settings.RandomSeed,
                    now,
                    settings.Profile),
                cancellationToken);
            logger.LogInformation(
                "Demo CRM ready: {Contacts} contacts, {Orders} orders, {Deals} deals, {Tasks} tasks, {Notes} notes.",
                crmResult.ContactsInserted,
                crmResult.OrdersInserted,
                crmResult.DealsInserted,
                crmResult.TasksInserted,
                crmResult.NotesInserted);

            logger.LogInformation("Demo seed step: campaign history and planned campaigns.");
            var campaignResult = await campaigns.SeedAsync(
                organizationId,
                crmResult.LatestOrderAt ?? now.AddDays(-1),
                cancellationToken);
            logger.LogInformation(
                "Demo campaigns ready: {InjectedOrders} simulated orders from the injected campaign.",
                campaignResult.SimulatedOrdersWritten);

            logger.LogInformation("Demo seed step: inbox notifications.");
            await notifications.SeedAsync(
                organizationId,
                identityResult.Accounts.Select(account => account.UserId).ToArray(),
                identityResult.Accounts[0].UserId,
                now,
                cancellationToken);
            logger.LogInformation("Demo notifications ready: {Recipients} recipients.", identityResult.Accounts.Count);

            DemoInsightsReport? insightsResult = null;
            try
            {
                logger.LogInformation("Demo seed step: RFM snapshot, scoring and priority list.");
                insightsResult = await insights.SeedAsync(organizationId, now, cancellationToken);
                logger.LogInformation(
                    "Demo insights ready: RFM={Rfm}, priority={Priority}, ML={Ml}, ML note={MlFailure}.",
                    insightsResult.RfmBuilt,
                    insightsResult.PriorityBuilt,
                    insightsResult.MlEnabled,
                    insightsResult.MlFailure ?? "none");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "Demo insights step failed with {ExceptionType}; the CRM data is seeded and the daily job will build the list.",
                    exception.GetType().Name);
            }

            await identity.MarkSeededAsync(organizationId, now.UtcDateTime, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("Demo workspace {Status} in {Duration}.", regenerating ? "regenerated" : "seeded", stopwatch.Elapsed);
            return new DemoSeedOutcome(
                regenerating ? DemoSeedOutcome.Regenerated : DemoSeedOutcome.Seeded,
                stopwatch.Elapsed,
                identityResult,
                crmResult,
                campaignResult,
                insightsResult);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogError(
                "Demo seed failed with {ExceptionType} after {Duration}; the marker is unchanged, so the next start retries.",
                exception.GetType().Name,
                stopwatch.Elapsed);
            return new DemoSeedOutcome(DemoSeedOutcome.Failed, stopwatch.Elapsed);
        }
    }
}
