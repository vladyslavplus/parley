namespace Parley.SharedKernel.Tenancy.Exceptions;

/// <summary>
/// Thrown when a tenant entity is accessed without an active tenant.
/// </summary>
public sealed class TenantContextNotSetException : InvalidOperationException
{
    private const string MessageText =
        "A tenant entity was accessed without an active tenant. " +
        "ITenantContext must provide an organization for the current HTTP request or job scope. " +
        "Tenant isolation is fail-closed: no rows are returned and no write is performed without a tenant.";

    public TenantContextNotSetException() : base(MessageText)
    {
    }
}
