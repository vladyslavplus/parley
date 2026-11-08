using Microsoft.AspNetCore.Mvc;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Errors;

namespace Parley.Api.Idempotency;

/// <summary>
/// Opt-in <c>Idempotency-Key</c> support for a mutating CRM endpoint, bound to the CRM module's store.
/// The message sequence for one key and one organization:
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
/// Requests without the header are not idempotent and proceed normally. Each module that accepts keys
/// names its own attribute, bound to its own <c>DbContext</c>, so keys land in that module's schema.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CrmIdempotencyKeyAttribute() : ServiceFilterAttribute(typeof(IdempotencyKeyFilter<CrmDbContext>));
