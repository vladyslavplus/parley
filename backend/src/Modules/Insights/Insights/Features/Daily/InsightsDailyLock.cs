using Npgsql;

namespace Parley.Insights.Features.Daily;

/// <summary>
/// Session advisory lock of one organization day. Two runs of the same day cannot interleave: the second run finds the lock
/// taken and leaves at once, and the sweep queues it again while the day is incomplete. The lock is held on its own
/// connection, so the steps of the run may use any connection of the pool.
/// </summary>
public sealed class InsightsDailyLock(NpgsqlDataSource dataSource)
{
    private const string LockNamespace = "parley.insights.daily";

    private const string TryAcquireSql = "SELECT pg_try_advisory_lock(hashtext(@namespace), hashtext(@key))";

    private const string ReleaseSql = "SELECT pg_advisory_unlock(hashtext(@namespace), hashtext(@key))";

    /// <summary>Takes the lock of the day, or returns null when another run holds it. Dispose the result to release it.</summary>
    public async Task<IAsyncDisposable?> TryAcquireAsync(Guid organizationId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var key = $"{organizationId:D}:{asOf:yyyy-MM-dd}";
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = new NpgsqlCommand(TryAcquireSql, connection);
            command.Parameters.AddWithValue("namespace", LockNamespace);
            command.Parameters.AddWithValue("key", key);
            var acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
            if (!acquired)
            {
                await connection.DisposeAsync();
                return null;
            }

            return new Lease(connection, key);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Lease(NpgsqlConnection connection, string key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using (connection)
            {
                await using var command = new NpgsqlCommand(ReleaseSql, connection);
                command.Parameters.AddWithValue("namespace", LockNamespace);
                command.Parameters.AddWithValue("key", key);
                await command.ExecuteScalarAsync(CancellationToken.None);
            }
        }
    }
}
