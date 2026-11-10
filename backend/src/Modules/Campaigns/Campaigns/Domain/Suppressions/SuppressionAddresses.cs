namespace Parley.Campaigns.Domain.Suppressions;

/// <summary>
/// Normalization of an address for suppression matching. It is the same rule the CRM applies to its stored address
/// (trim, then lowercase), so a suppression written from a recipient matches the contact at the freeze.
/// </summary>
public static class SuppressionAddresses
{
    /// <summary>Normalized address, or null when the address is blank.</summary>
    public static string? Normalize(string? address)
    {
        var trimmed = address?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }
}
