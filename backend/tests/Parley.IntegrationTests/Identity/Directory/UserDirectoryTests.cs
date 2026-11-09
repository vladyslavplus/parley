using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Parley.Identity.Contracts.Users;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Identity.Directory;

/// <summary>
/// The user directory resolves only members of the requested organization. A user from another
/// organization never resolves, even when the caller asks for that user by id.
/// </summary>
public sealed class UserDirectoryTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string OwnerName = "Olena Shevchenko";
    private const string OtherName = "Other Organization Person";

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
    public async Task FindMembers_ResolvesOnlyUsersOfTheRequestedOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, owner.UserId, OwnerName, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, other.UserId, OtherName, token);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
        var directory = scope.ServiceProvider.GetRequiredService<IUserDirectory>();

        var resolved = await directory.FindMembersAsync(owner.OrganizationId, [owner.UserId, other.UserId], token);

        resolved.Should().ContainKey(owner.UserId);
        resolved.Should().NotContainKey(other.UserId);
        resolved[owner.UserId].Should().Be(new UserDirectoryEntry(owner.UserId, OwnerName, owner.Email, IsActive: true));

        var fromOtherOrganization = await directory.FindMembersAsync(other.OrganizationId, [owner.UserId], token);
        fromOtherOrganization.Should().BeEmpty();
    }
}
