using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.SharedKernel.Persistence.Concurrency;

/// <summary>
/// Recognizes a unique-index violation raised by a save, matched by the index name. Lets a
/// handler turn a lost race on a unique key into the conflict it reports for that key.
/// </summary>
public static class UniqueConstraintViolations
{
    public static bool IsViolationOf(DbUpdateException exception, string constraintName)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception.InnerException; current is not null; current = current.InnerException)
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
