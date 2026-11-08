using Microsoft.AspNetCore.Http;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Tenancy.Implementations;

/// <summary>
/// The single tenant resolved by DbContexts, interceptors and stores. The
/// ambient job organization wins when set (background jobs and the outbox
/// dispatcher set it per row); otherwise the organization comes from the
/// HTTP request claim. When neither exists reading the organization throws
/// <see cref="TenantContextNotSetException"/> (fail-closed).
/// </summary>
public sealed class CompositeTenantContext(
    JobTenantContext ambient,
    IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid OrganizationId
    {
        get
        {
            if (ambient.TryGetOrganizationId(out var organizationId))
            {
                return organizationId;
            }

            var claim = httpContextAccessor.HttpContext?.User.FindFirst(TenancyClaimTypes.OrganizationId)?.Value;
            return Guid.TryParse(claim, out var httpOrganizationId)
                ? httpOrganizationId
                : throw new TenantContextNotSetException();
        }
    }
}
