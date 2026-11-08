using Parley.SharedKernel.Idempotency.Persistence;

namespace Parley.SharedKernel.Idempotency.Abstractions;

/// <summary>
/// Claim and replay operations for <c>Idempotency-Key</c> rows. The caller
/// (the API filter) sets the tenant; the organization of every row is read
/// from <c>ITenantContext</c>, so a key always belongs to the organization
/// that claimed it. All mutations are keyed by the claim row identity
/// (<c>Id</c>), never by the client-provided key alone, so a stale caller
/// can never delete or overwrite a fresh claim that replaced an expired row.
/// Set-based SQL (<c>ExecuteDelete</c>/<c>ExecuteUpdate</c>) is used for
/// completion and deletion so idempotency code never flushes the request's
/// tracked graph (a failed handler's partial changes are never saved by
/// releasing the claim).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Finds the row for <paramref name="key"/> under the current tenant.</summary>
    Task<IdempotencyKey?> FindAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a claim row before the handler runs. Returns the new claim
    /// identity <c>Id</c> when this caller claimed the key, <c>null</c> when
    /// the unique constraint says another request claimed it first.
    /// </summary>
    Task<Guid?> TryClaimAsync(
        string key,
        byte[] requestHash,
        DateTime expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Stores the produced response on the claim row identified by <paramref name="claimId"/>.</summary>
    Task CompleteClaimAsync(
        Guid claimId,
        int responseStatus,
        byte[]? responseBody,
        string? responseContentType,
        string? responseHeaders,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the claim row identified by <paramref name="claimId"/> without a
    /// response, e.g. when the handler failed and the key must become reusable
    /// right away.
    /// </summary>
    Task DeleteClaimAsync(Guid claimId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically removes an expired claim row only if it is still the same
    /// row and still expired. Returns <c>true</c> when a row was removed. A
    /// concurrent takeover that already replaced the row is never removed.
    /// </summary>
    Task<bool> TryDeleteExpiredClaimAsync(
        Guid claimId,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
