using System.Globalization;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Options;
using Parley.Api.Options;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Features.Campaigns.Dispatch.Job;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Options;
using Parley.Crm.Exports;
using Parley.Crm.Tasks;
using Parley.Insights.Features.Daily;
using Parley.SharedKernel.BackgroundJobs;

namespace Parley.Api.Hangfire;

/// <summary>
/// One-time post-init for Hangfire storage. Hangfire owns its tables (they are
/// not EF-managed), so this hosted service: installs the storage schema
/// idempotently, applies the autovacuum tuning from
/// <see cref="HangfirePostInitSql"/>, and declares the recurring jobs (idempotency
/// cleanup, export cleanup, task reminders, campaign send recovery). It runs before the background server starts and
/// retries on transient failures without failing the host, so an unavailable database
/// never takes the API down.
/// </summary>
public sealed class HangfireSchemaInitializer(
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<CampaignsSendOptions> campaignSendOptions,
    IOptions<CampaignsPrepareOptions> campaignPrepareOptions,
    IRecurringJobManager recurringJobs,
    TimeProvider timeProvider,
    ILogger<HangfireSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await InstallAsync(cancellationToken);
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                attempts++;
                if (attempts >= HangfireConstants.SchemaInitMaxAttempts)
                {
                    logger.LogError(
                        exception,
                        "Hangfire schema initialization failed after {Attempts} attempts. The background server will retry fetching jobs.",
                        attempts);
                    break;
                }

                logger.LogWarning(
                    exception,
                    "Hangfire schema initialization failed (attempt {Attempts} of {MaxAttempts}). Retrying.",
                    attempts,
                    HangfireConstants.SchemaInitMaxAttempts);
                await Task.Delay(HangfireOptions.DefaultSchemaInitRetryDelay, timeProvider, cancellationToken);
            }
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.IdempotencyCleanupJobName,
                Job.FromExpression<IdempotencyCleanupJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.IdempotencyCleanupCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring idempotency cleanup registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.ContactExportCleanupJobName,
                Job.FromExpression<ContactExportCleanupJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.ContactExportCleanupCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring contact export cleanup registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.TaskDueSoonScanJobName,
                Job.FromExpression<TaskDueSoonScanJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.TaskDueSoonScanCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring task due-soon registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.CampaignSendRecoveryJobName,
                Job.FromExpression<CampaignSendRecoveryJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                IntervalCron(campaignSendOptions.Value.RecoveryInterval),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring campaign send recovery registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.CampaignAttributionSweepJobName,
                Job.FromExpression<CampaignAttributionSweepJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.DailyAttributionCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring campaign attribution registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.InsightsDailySweepJobName,
                Job.FromExpression<InsightsDailySweepJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.HourlyCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring insights daily sweep registration was deferred until the database is available.");
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.CampaignPrepareRecoveryJobName,
                Job.FromExpression<CampaignPrepareRecoveryJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                IntervalCron(campaignPrepareOptions.Value.RecoveryInterval),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring campaign prepare recovery registration was deferred until the database is available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>The interval as a cron expression. The options validate it as whole minutes between one minute and one hour.</summary>
    private static string IntervalCron(TimeSpan interval) =>
        interval >= TimeSpan.FromHours(1)
            ? HangfireConstants.HourlyCron
            : string.Format(CultureInfo.InvariantCulture, HangfireConstants.MinuteStepCronFormat, (int)interval.TotalMinutes);

    private Task InstallAsync(CancellationToken cancellationToken) =>
        HangfireSchemaInstaller.InstallAsync(databaseOptions.Value.ConnectionString, cancellationToken);
}
