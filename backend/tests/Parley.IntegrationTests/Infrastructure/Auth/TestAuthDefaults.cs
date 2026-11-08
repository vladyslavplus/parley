namespace Parley.IntegrationTests.Infrastructure.Auth;

/// <summary>
/// Constants for the test authentication scheme.
/// </summary>
public static class TestAuthDefaults
{
    public const string AuthenticationScheme = "Test";

    public const string OrganizationHeader = "X-Test-Org-Id";

    public const string UserHeader = "X-Test-User-Id";

    /// <summary>When set to "true", the test principal carries the platform-operator claim.</summary>
    public const string PlatformOperatorHeader = "X-Test-Platform-Operator";
}
