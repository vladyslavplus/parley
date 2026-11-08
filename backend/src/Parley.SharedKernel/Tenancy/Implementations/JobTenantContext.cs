using Microsoft.AspNetCore.Http;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Tenancy.Implementations;

/// <summary>
/// Tenant for background work. Settable per execution flow so parallel jobs
/// never share one tenant implicitly. All instances resolve through the same
/// ambient execution-flow value (like <see cref="IHttpContextAccessor"/>):
/// any instance that helps build an EF model observes the tenant set by the
/// caller on the executing flow.
/// </summary>
public sealed class JobTenantContext : ITenantContext, IMutableTenantContext
{
    private static readonly AsyncLocal<Guid?> AmbientOrganizationId = new();

    public Guid OrganizationId => AmbientOrganizationId.Value ?? throw new TenantContextNotSetException();

    /// <summary>Reads the ambient organization without throwing when none is set.</summary>
    public bool TryGetOrganizationId(out Guid organizationId)
    {
        var ambient = AmbientOrganizationId.Value;
        organizationId = ambient ?? Guid.Empty;
        return ambient.HasValue;
    }

    public void Set(Guid organizationId) => AmbientOrganizationId.Value = organizationId;

    public void Clear() => AmbientOrganizationId.Value = null;
}
