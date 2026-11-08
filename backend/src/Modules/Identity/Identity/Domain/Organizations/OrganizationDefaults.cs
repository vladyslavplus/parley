using System.Security.Cryptography;

namespace Parley.Identity.Domain.Organizations;

/// <summary>
/// Values a new organization starts with. Settings can be changed later by an
/// administrator; the slug is assigned once.
/// </summary>
public static class OrganizationDefaults
{
    public const string TimeZone = "UTC";

    public const string Currency = "UAH";

    private const string SlugPrefix = "org-";

    /// <summary>Random bytes behind the slug suffix (48 bits, rendered as 12 hex characters).</summary>
    private const int SlugRandomBytes = 6;

    /// <summary>Returns a new slug. The random suffix is not time-based, so slugs created in the same millisecond differ.</summary>
    public static string NewSlug() =>
        SlugPrefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SlugRandomBytes));
}
