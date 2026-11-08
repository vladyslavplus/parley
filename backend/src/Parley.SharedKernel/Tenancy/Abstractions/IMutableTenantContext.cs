namespace Parley.SharedKernel.Tenancy.Abstractions;

/// <summary>
/// Settable tenant for background work. The outbox dispatcher resolves this
/// service and assigns the organization of each claimed row before delivery.
/// </summary>
public interface IMutableTenantContext : ITenantContext
{
    void Set(Guid organizationId);

    void Clear();
}
