using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.SharedKernel.BackgroundJobs;

/// <summary>
/// Idempotent post-init SQL for the Hangfire schema. Hangfire owns its tables
/// (they are not EF-managed), so the tuning they need ships as plain SQL that
/// the host applies once the schema exists. Every statement is safe to run
/// repeatedly: <c>ALTER TABLE ... SET</c> overwrites the same settings.
///
/// The three hot tables are <c>job</c>, <c>state</c> and <c>counter</c>; the
/// tighter autovacuum thresholds keep MVCC bloat from those churn-heavy tables
/// under control.
/// </summary>
public static class HangfirePostInitSql
{
    private static readonly string Schema = PersistenceConstants.HangfireSchema;

    private const int AutovacuumVacuumThreshold = 100;

    private const double AutovacuumVacuumScaleFactor = 0.05;

    public static IReadOnlyList<string> AutovacuumStatements { get; } =
    [
        $"ALTER TABLE \"{Schema}\".\"job\" SET (autovacuum_vacuum_threshold = {AutovacuumVacuumThreshold}, autovacuum_vacuum_scale_factor = {AutovacuumVacuumScaleFactor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})",
        $"ALTER TABLE \"{Schema}\".\"state\" SET (autovacuum_vacuum_threshold = {AutovacuumVacuumThreshold}, autovacuum_vacuum_scale_factor = {AutovacuumVacuumScaleFactor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})",
        $"ALTER TABLE \"{Schema}\".\"counter\" SET (autovacuum_vacuum_threshold = {AutovacuumVacuumThreshold}, autovacuum_vacuum_scale_factor = {AutovacuumVacuumScaleFactor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})",
    ];
}
