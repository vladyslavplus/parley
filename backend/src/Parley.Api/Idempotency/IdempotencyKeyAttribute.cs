using Microsoft.AspNetCore.Mvc;
using Parley.SharedKernel.Errors;

namespace Parley.Api.Idempotency;

/// <summary>
/// Opt-in <c>Idempotency-Key</c> support for a mutating endpoint. The message
/// sequence for one key and one organization:
/// <list type="number">
/// <item>The first request claims the key (a row insert decided by the unique
/// constraint), then runs the handler once and stores the response.</item>
/// <item>A concurrent request with the same key polls briefly for the stored
/// response instead of running the handler again.</item>
/// <item>A later request with the same key and the same fingerprint replays
/// the stored response; the handler never runs again.</item>
/// <item>The same key with a different fingerprint (method, path, query, user
/// id or body) is a 409 <see cref="ErrorCodes.IdempotencyKeyReused"/> problem,
/// never a second execution.</item>
/// </list>
/// Requests without the header are not idempotent and proceed normally.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IdempotencyKeyAttribute() : ServiceFilterAttribute(typeof(IdempotencyKeyFilter));
