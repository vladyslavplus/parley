using Parley.SharedKernel.Persistence.Entities;

namespace Parley.SharedKernel.Idempotency.Persistence;

/// <summary>
/// A stored <c>Idempotency-Key</c> claim plus the first response. The row is
/// inserted as a claim <em>before</em> the handler runs, so concurrent first
/// use of one key runs the handler once: the unique constraint on
/// <c>(organization_id, key)</c> decides the claim, and later callers replay
/// the stored response.
///
/// The response body is stored as raw bytes (not text or JSON): replay must
/// return the exact bytes the handler produced (including binary bodies),
/// and allow-listed response headers round-trip through
/// <see cref="ResponseHeaders"/> so <c>Location</c> and <c>ETag</c> survive
/// replay.
/// </summary>
public sealed class IdempotencyKey : BusinessEntity
{
    public IdempotencyKey()
    {
    }

    public IdempotencyKey(Guid organizationId, string key, byte[] requestHash, DateTime expiresAt)
        : base(organizationId)
    {
        Key = key;
        RequestHash = requestHash;
        ExpiresAt = expiresAt;
    }

    /// <summary>The client-provided <c>Idempotency-Key</c> header value.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Hash of method, path, query, user id and body that claimed this key.</summary>
    public byte[] RequestHash { get; set; } = [];

    /// <summary>
    /// Stored response status. <see cref="IdempotencyTableNames.ClaimedStatus"/>
    /// means the handler is still running and nothing is stored yet.
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>Stored response body bytes, for byte-exact replay.</summary>
    public byte[]? ResponseBody { get; set; }

    /// <summary>Stored response content type, for replay.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>
    /// Allow-listed response headers as a JSON object (for example
    /// <c>{"Location":"...","ETag":"..."}</c>), for replay.
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>After this instant the key may be claimed again.</summary>
    public DateTime ExpiresAt { get; set; }
}
