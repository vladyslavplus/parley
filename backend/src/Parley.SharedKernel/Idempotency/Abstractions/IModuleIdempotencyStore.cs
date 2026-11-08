using Microsoft.EntityFrameworkCore;

namespace Parley.SharedKernel.Idempotency.Abstractions;

/// <summary>
/// The idempotency store of one module, bound to that module's <typeparamref name="TDbContext"/>. Each module
/// registers its own (as with the outbox), so keys land in that module's schema and a second module's
/// registration never replaces the first. Endpoints name their module through its attribute.
/// </summary>
public interface IModuleIdempotencyStore<TDbContext> : IIdempotencyStore
    where TDbContext : DbContext;
