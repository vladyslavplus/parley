using Microsoft.AspNetCore.Mvc;
using Parley.Campaigns.Persistence;

namespace Parley.Api.Idempotency;

/// <summary>
/// Opt-in <c>Idempotency-Key</c> support for a mutating campaigns endpoint, bound to the campaigns module's store. The
/// rules are those of <see cref="CrmIdempotencyKeyAttribute"/>: a repeat with the same key replays the stored response,
/// and the same key with another request is a conflict.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CampaignsIdempotencyKeyAttribute() : ServiceFilterAttribute(typeof(IdempotencyKeyFilter<CampaignsDbContext>));
