namespace Parley.SharedKernel.Idempotency.Persistence;

/// <summary>
/// Schema and table constants for <c>Idempotency-Key</c> storage. Each module
/// maps the shared row into its own schema through
/// <see cref="IdempotencyModelBuilderExtensions.AddIdempotencyConventions"/>.
/// </summary>
public static class IdempotencyTableNames
{
    public const string IdempotencyKeys = "idempotency_keys";

    public const string UniqueKeyIndexName = "uq_idempotency_keys_organization_key";

    public const string ExpiresIndexName = "ix_idempotency_keys_expires_at";

    /// <summary>Status stored while the first request is still running.</summary>
    public const int ClaimedStatus = 0;
}
