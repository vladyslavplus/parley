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

    public const int SchemaInitMaxAttempts = 10;

    /// <summary>
    /// PostgreSQL advisory lock key serializing schema installs, so a cold
    /// start by several hosts at once cannot install half a schema.
    /// </summary>
    public const long SchemaInstallLockKey = 840193027;
}
