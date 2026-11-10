namespace Parley.Api.Hangfire;

/// <summary>
/// Names for Hangfire wiring shared by the host and its registrations.
/// </summary>
public static class HangfireConstants
{
    public const string IdempotencyCleanupJobName = "idempotency-keys-cleanup";

    public const string IdempotencyCleanupCron = "*/15 * * * *";

    public const string ContactExportCleanupJobName = "crm-contact-exports-cleanup";

    public const string ContactExportCleanupCron = "*/15 * * * *";

    public const string TaskDueSoonScanJobName = "crm-task-due-soon-scan";

    public const string TaskDueSoonScanCron = "*/5 * * * *";

    public const string CampaignSendRecoveryJobName = "campaigns-send-recovery";

    public const string CampaignPrepareRecoveryJobName = "campaigns-prepare-recovery";

    public const string CampaignAttributionSweepJobName = "campaigns-attribution-daily";

    public const string InsightsDailySweepJobName = "insights-daily-sweep";

    /// <summary>Cron of the daily attribution sweep, 04:00 UTC.</summary>
    public const string DailyAttributionCron = "0 4 * * *";

    /// <summary>Cron of an hourly sweep. Intervals under an hour are built from <see cref="MinuteStepCronFormat"/>.</summary>
    public const string HourlyCron = "0 * * * *";

    /// <summary>Cron format of a minute step, with the number of minutes as its argument.</summary>
    public const string MinuteStepCronFormat = "*/{0} * * * *";

    public const int SchemaInitMaxAttempts = 10;

    /// <summary>
    /// PostgreSQL advisory lock key serializing schema installs, so a cold
    /// start by several hosts at once cannot install half a schema.
    /// </summary>
    public const long SchemaInstallLockKey = 840193027;
}
