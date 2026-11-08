using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Infrastructure.Auth;

/// <summary>
/// Creates an authenticated client whose tenant comes from the
/// <see cref="TestAuthDefaults.OrganizationHeader"/> and user from the
/// <see cref="TestAuthDefaults.UserHeader"/> header. Endpoints without
/// authorization stay anonymous regardless of headers.
/// </summary>
public static class TestAuthClientExtensions
{
    public static HttpClient CreateClientWithTenant(
        this Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory,
        Guid organizationId,
        Guid userId,
        bool platformOperator = false)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.OrganizationHeader, organizationId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthDefaults.UserHeader, userId.ToString());
        if (platformOperator)
        {
            client.DefaultRequestHeaders.Add(TestAuthDefaults.PlatformOperatorHeader, TenancyClaimTypes.PlatformOperatorValue);
        }

        return client;
    }
}
