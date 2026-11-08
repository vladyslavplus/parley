namespace Parley.SharedKernel.Tenancy.Exceptions;

/// <summary>
/// Thrown when an entity is written under a tenant different from the active tenant.
/// </summary>
public sealed class TenantMismatchException : InvalidOperationException
{
    private const string MessageText =
        "An entity was written under an organization that does not match the active tenant. " +
        "Tenant writes must carry the organization of the current HTTP request or job scope.";

    public TenantMismatchException() : base(MessageText)
    {
    }
}
