using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Access;

/// <summary>
/// Server-side permission enforcement on an authenticated request: the forbidden error
/// with its localized ProblemDetails, the membership read on every request, and the
/// role-grant cache with its invalidation.
/// </summary>
public sealed class PermissionEnforcementTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string UkForbiddenDetail = "Недостатньо прав для цієї дії.";

    private const string EnForbiddenDetail = "You do not have permission to do this.";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(CultureCodes.Ukrainian, UkForbiddenDetail)]
    [InlineData(CultureCodes.English, EnForbiddenDetail)]
    public async Task ProtectedRequest_WithoutPermission_ReturnsForbiddenProblemDetailsInRequestedLanguage(string language, string expectedDetail)
    {
        var token = TestContext.Current.CancellationToken;
        var ownerClient = CreateClient(_factory, CultureCodes.English);
        var owner = await CreateConfirmedAccountAsync(ownerClient, mailpit, token);
        var organizationId = await AccessTestData.ReadOrganizationIdAsync(_factory.Services, owner.UserId, token);

        // The viewer role grants read access only, so contacts.write is missing.
        var viewerEmail = UniqueEmail();
        await AccessTestData.AddMemberAsync(_factory.Services, organizationId, SystemRoleSeed.ViewerRoleId, viewerEmail, _factory.Clock, token);
        var viewerAccess = await SignInAndReadAccessTokenAsync(viewerEmail, token);

        using var client = CreateClient(_factory, language);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerAccess);

        var response = await client.GetAsync(PermissionProbeRoutes.Probe, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await ReadJsonAsync(response, token);
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.PermissionDenied);
        body.GetProperty("detail").GetString().Should().Be(expectedDetail);
    }

    [Fact]
    public async Task ProtectedRequest_AfterMembershipDeactivation_RejectsTheOldToken()
    {
        var token = TestContext.Current.CancellationToken;
        var ownerClient = CreateClient(_factory, CultureCodes.English);
        var owner = await CreateConfirmedAccountAsync(ownerClient, mailpit, token);
        var organizationId = await AccessTestData.ReadOrganizationIdAsync(_factory.Services, owner.UserId, token);

        var memberEmail = UniqueEmail();
        var memberId = await AccessTestData.AddMemberAsync(_factory.Services, organizationId, SystemRoleSeed.AdminRoleId, memberEmail, _factory.Clock, token);
        var memberAccess = await SignInAndReadAccessTokenAsync(memberEmail, token);

        using var client = CreateClient(_factory, CultureCodes.English);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccess);
        (await client.GetAsync(PermissionProbeRoutes.Probe, token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AccessTestData.SetMembershipStatusAsync(_factory.Services, organizationId, memberId, MembershipStatuses.Deactivated, token);

        // The same access token is still valid for signature and lifetime; only the membership changed.
        (await client.GetAsync(PermissionProbeRoutes.Probe, token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProtectedRequest_RoleGrantChange_TakesEffectOnlyAfterCacheInvalidation()
    {
        var token = TestContext.Current.CancellationToken;
        var ownerClient = CreateClient(_factory, CultureCodes.English);
        var owner = await CreateConfirmedAccountAsync(ownerClient, mailpit, token);
        var organizationId = await AccessTestData.ReadOrganizationIdAsync(_factory.Services, owner.UserId, token);

        var roleId = await AccessTestData.AddCustomRoleAsync(_factory.Services, organizationId, "probe_writer", Permissions.ContactsWrite, token);
        var memberEmail = UniqueEmail();
        await AccessTestData.AddMemberAsync(_factory.Services, organizationId, roleId, memberEmail, _factory.Clock, token);
        var memberAccess = await SignInAndReadAccessTokenAsync(memberEmail, token);

        using var client = CreateClient(_factory, CultureCodes.English);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccess);
        (await client.GetAsync(PermissionProbeRoutes.Probe, token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The grant is removed in the database; the cached permission set still allows the request.
        await AccessTestData.RemoveGrantAsync(_factory.Services, roleId, Permissions.ContactsWrite, token);
        (await client.GetAsync(PermissionProbeRoutes.Probe, token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();
            await checker.InvalidateRolePermissionsAsync(roleId, token);
        }

        (await client.GetAsync(PermissionProbeRoutes.Probe, token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> SignInAndReadAccessTokenAsync(string email, CancellationToken cancellationToken)
    {
        using var client = CreateClient(_factory, CultureCodes.English);
        var response = await SignInAsync(client, email, Password, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await ReadTokensAsync(response, cancellationToken)).AccessToken;
    }
}
