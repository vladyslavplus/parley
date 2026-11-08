namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Test-only endpoints, registered as an application part by the integration test
/// factories. They never ship in the API host.
/// </summary>
public static class TestOnlyRoutes
{
    public const string Throw = "test-only/throw";

    public const string CurrentPrincipal = "test-only/me";

    public const string ThrowMessage = "test-only diagnostics failure";
}
