namespace Parley.SharedKernel.Tenancy.Abstractions;

/// <summary>
/// Implemented by module DbContexts applying tenant conventions. The tenant
/// context is exposed as a DbContext member so query filters read the
/// executing context's tenant on every query instead of the value captured
/// when the model was built.
/// </summary>
public interface ITenantDbContext
{
    ITenantContext TenantContext { get; }
}
