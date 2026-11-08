using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Auth;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Events;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Outbox.Messages;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Access;

/// <summary>
/// Registration creates the organization, the admin membership and the organization
/// event in one transaction. A failure at the last write leaves none of them behind.
/// </summary>
public sealed class RegistrationAccessTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string RegisteredOrganizationName = "Test organization";

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

    [Fact]
    public async Task Registration_CreatesOrganizationAdminMembershipAndEvent()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = UniqueEmail();

        var registered = await RegisterAsync(client, email, Password, token);
        registered.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.SingleAsync(candidate => candidate.Email == email, token);
        var membership = await db.Memberships.IgnoreQueryFilters().SingleAsync(row => row.UserId == user.Id, token);
        membership.RoleId.Should().Be(SystemRoleSeed.AdminRoleId);
        membership.Status.Should().Be(MembershipStatuses.Active);

        var organization = await db.Organizations.SingleAsync(row => row.Id == membership.OrganizationId, token);
        organization.Name.Should().Be(RegisteredOrganizationName);

        var organizationEvents = await db.Set<OutboxMessage>()
            .Where(message => message.OrganizationId == organization.Id)
            .ToListAsync(token);
        organizationEvents.Should().ContainSingle()
            .Which.Type.Should().Be(typeof(OrganizationCreated).FullName);
    }

    [Fact]
    public async Task Registration_FailingOutboxWrite_RollsBackOrganizationAndUser()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = UniqueEmail();
        var organizationName = RegistrationRollbackTrigger.OrganizationNamePrefix + Guid.CreateVersion7().ToString("N");

        var response = await client.PostAsJsonAsync(
            Route(AuthRoutes.Register),
            new { email, password = Password, organizationName },
            token);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await db.Users.AnyAsync(candidate => candidate.Email == email, token)).Should().BeFalse();
        (await db.Organizations.AnyAsync(row => row.Name == organizationName, token)).Should().BeFalse();
    }
}
