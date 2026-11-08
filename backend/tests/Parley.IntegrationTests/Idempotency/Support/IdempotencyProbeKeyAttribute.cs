using Microsoft.AspNetCore.Mvc;
using Parley.Api.Idempotency;
using Parley.IntegrationTests.Tenancy.Support;

namespace Parley.IntegrationTests.Idempotency.Support;

/// <summary>Idempotency attribute of the probe endpoints, bound to the test DbContext's store.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IdempotencyProbeKeyAttribute() : ServiceFilterAttribute(typeof(IdempotencyKeyFilter<TenantTestDbContext>));
