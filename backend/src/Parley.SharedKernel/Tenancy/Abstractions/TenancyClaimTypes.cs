namespace Parley.SharedKernel.Tenancy.Abstractions;

/// <summary>
/// Claim types carried by tokens produced for tenant work.
/// </summary>
public static class TenancyClaimTypes
{
    public const string OrganizationId = "org";
    public const string UserId = "sub";

    /// <summary>
    /// Marks a principal as a platform operator: the only users allowed to open
    /// the Hangfire dashboard, because job storage is instance-wide and holds
    /// arguments for every organization.
    /// </summary>
    public const string PlatformOperator = "is_platform_operator";

    /// <summary>Value of the <see cref="PlatformOperator"/> claim marking an operator.</summary>
    public const string PlatformOperatorValue = "true";
}
