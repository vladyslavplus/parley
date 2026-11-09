using Microsoft.EntityFrameworkCore;

namespace Parley.SharedKernel.Outbox.Publishing;

/// <summary>
/// The outbox of one module database. Each module resolves its own outbox, so an
/// event always lands in the outbox table of the context that saves the business change.
/// </summary>
/// <typeparam name="TDbContext">The module DbContext that owns the outbox table.</typeparam>
public interface IModuleOutbox<TDbContext> : IOutbox
    where TDbContext : DbContext;
