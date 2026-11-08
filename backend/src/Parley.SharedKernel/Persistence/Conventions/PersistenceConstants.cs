namespace Parley.SharedKernel.Persistence.Conventions;

/// <summary>
/// Persistence constants shared across modules.
/// </summary>
public static class PersistenceConstants
{
    public const string XminPropertyName = "xmin";

    private const string SerializationFailureSqlState = "40001";

    private const string DeadlockDetectedSqlState = "40P01";

    public const string UniqueViolationSqlState = "23505";

    public static IReadOnlyCollection<string> PostgreSqlErrorCodesToRetry { get; } =
        [SerializationFailureSqlState, DeadlockDetectedSqlState];

    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    public const string HangfireSchema = "hangfire";
}
