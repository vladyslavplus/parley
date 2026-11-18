namespace Parley.Identity.Contracts.Organizations;

/// <summary>
/// Reads the ISO 4217 currency of the current tenant's organization. A user-facing read uses it to label a money amount
/// with the currency the organization works in; the platform-wide directory is for jobs only.
/// </summary>
public interface IOrganizationCurrencyReader
{
    /// <summary>The organization currency; null when the tenant has no organization row.</summary>
    Task<string?> ReadAsync(CancellationToken cancellationToken);
}
