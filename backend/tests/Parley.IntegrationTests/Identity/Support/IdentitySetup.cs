using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;

namespace Parley.IntegrationTests.Identity.Support;

/// <summary>
/// Shared arrangement for identity tests: an owner account created through registration,
/// and members added directly with a role.
/// </summary>
public static class IdentitySetup
{
    public static async Task<OrganizationOwner> CreateOwnerAsync(IdentityApiFactory factory, MailpitFixture mailpit, CancellationToken cancellationToken)
    {
        using var client = AuthTestClient.CreateClient(factory, CultureCodes.English);
        var account = await AuthTestClient.CreateConfirmedAccountAsync(client, mailpit, cancellationToken);
        var organizationId = await AccessTestData.ReadOrganizationIdAsync(factory.Services, account.UserId, cancellationToken);
        var tokens = await IdentityApi.SignInAsync(factory, account.Email, cancellationToken);
        return new OrganizationOwner(account.Email, account.UserId, organizationId, tokens.AccessToken);
    }

    /// <summary>Adds a confirmed member with the role, without an invitation. Returns the user identifier and address.</summary>
    public static async Task<(Guid UserId, string Email)> AddMemberAsync(
        IdentityApiFactory factory,
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var email = AuthTestClient.UniqueEmail();
        var userId = await AccessTestData.AddMemberAsync(factory.Services, organizationId, roleId, email, factory.Clock, cancellationToken);
        return (userId, email);
    }

    /// <summary>Sets the display name of a user. Registration and member creation leave it empty.</summary>
    public static async Task SetDisplayNameAsync(IdentityApiFactory factory, Guid userId, string displayName, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.DisplayName, displayName), cancellationToken);
    }
}
