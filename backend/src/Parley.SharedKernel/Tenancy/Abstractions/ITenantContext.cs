using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Tenancy.Abstractions;

/// <summary>
/// Ambient organization for the current scope. Every tenant query runs under this
/// context; reading the organization without an active tenant throws
/// <see cref="TenantContextNotSetException"/> instead of returning all rows.
/// </summary>
public interface ITenantContext
{
    Guid OrganizationId { get; }
}
