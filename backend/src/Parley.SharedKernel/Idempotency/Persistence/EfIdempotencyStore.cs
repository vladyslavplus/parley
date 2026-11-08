using Microsoft.EntityFrameworkCore;
using Npgsql;
using Parley.SharedKernel.Idempotency.Abstractions;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Idempotency.Persistence;

/// <summary>
/// <see cref="IIdempotencyStore"/> appending to the calling scope's
/// <typeparamref name="TDbContext"/>. The organization comes from
/// <c>ITenantContext</c> (fail-closed, an unset tenant throws
/// <see cref="TenantContextNotSetException"/>); the unique constraint is the
/// final guard for concurrent first use.
/// </summary>
public sealed class EfIdempotencyStore<TDbContext>(TDbContext context, ITenantContext tenantContext) : IIdempotencyStore
    where TDbContext : DbContext
{
    public async Task<IdempotencyKey?> FindAsync(string key, CancellationToken cancellationToken)
    {
        // No tracking: the filter polls for the stored response while the
        // claim row is being updated by another request, so every read must
        // see the freshest committed state instead of the identity map.
        return await context.Set<IdempotencyKey>()
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Key == key, cancellationToken);
    }

    public async Task<Guid?> TryClaimAsync(
        string key,
        byte[] requestHash,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        // Detach before claiming so the insert never flushes tracked graph
        // state that predates the claim (the claim commits on its own).
        // TryClaim runs before the handler, so the tracker is expected to be
        // empty; clearing makes the isolation explicit and detach-safe.
        context.ChangeTracker.Clear();
        var claim = new IdempotencyKey(organizationId, key, requestHash, expiresAt);
        context.Set<IdempotencyKey>().Add(claim);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return claim.Id;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            context.ChangeTracker.Clear();
            return null;
        }
    }

    public async Task CompleteClaimAsync(
        Guid claimId,
        int responseStatus,
        byte[]? responseBody,
        string? responseContentType,
        string? responseHeaders,
        CancellationToken cancellationToken)
    {
        // Direct UPDATE by claim identity: bypasses the change tracker, so a
        // completed response never flushes handler-tracked graph state.
        var updated = await context.Set<IdempotencyKey>()
            .Where(entry => entry.Id == claimId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entry => entry.ResponseStatus, responseStatus)
                    .SetProperty(entry => entry.ResponseBody, responseBody)
                    .SetProperty(entry => entry.ResponseContentType, responseContentType)
                    .SetProperty(entry => entry.ResponseHeaders, responseHeaders),
                cancellationToken);
        if (updated == 0)
        {
            throw new InvalidOperationException("The idempotency claim was not found when storing the response.");
        }
    }

    public async Task DeleteClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        // Direct DELETE by claim identity: never through the change tracker,
        // so releasing a claim never flushes the failed handler's tracked
        // partial changes and never conflicts with the tracked claim entity.
        await context.Set<IdempotencyKey>()
            .Where(entry => entry.Id == claimId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> TryDeleteExpiredClaimAsync(
        Guid claimId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var deleted = await context.Set<IdempotencyKey>()
            .Where(entry => entry.Id == claimId && entry.ExpiresAt <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        var current = exception.InnerException;
        while (current is not null)
        {
            if (current is PostgresException postgres &&
                string.Equals(postgres.SqlState, PersistenceConstants.UniqueViolationSqlState, StringComparison.Ordinal))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
