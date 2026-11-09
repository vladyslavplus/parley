using Npgsql;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.SharedKernel.Persistence.Concurrency;

/// <summary>
/// Recognizes a unique-index violation raised by a save, matched by the index name. Lets a
/// handler turn a lost race on a unique key into the conflict it reports for that key.
/// </summary>
public static class UniqueConstraintViolations
{
    /// <summary>Matches a save or a bulk update: the exception itself or any inner exception may carry the violation.</summary>
    public static bool IsViolationOf(Exception exception, string constraintName)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && string.Equals(postgres.SqlState, PersistenceConstants.UniqueViolationSqlState, StringComparison.Ordinal)
                && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
