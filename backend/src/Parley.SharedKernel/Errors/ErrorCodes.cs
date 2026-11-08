namespace Parley.SharedKernel.Errors;

/// <summary>
/// Stable error codes for shared failures.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "common.validation.failed";
    public const string ConcurrencyConflict = "common.concurrency.conflict";
    public const string RequestNotFound = "common.request.not_found";
    public const string Unexpected = "common.internal.unexpected";
    public const string IdempotencyKeyReused = "common.idempotency.key_reused";
    public const string IdempotencyInProgress = "common.idempotency.in_progress";
    public const string IdempotencyBodyTooLarge = "common.idempotency.body_too_large";
    public const string IdempotencyKeyTooLong = "common.idempotency.key_too_long";
}
