using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;
using Parley.Identity.Domain.Users;
using Parley.Identity.Persistence;
using Parley.Identity.Seeding.DemoSeed;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Identity.DemoSeed;

/// <summary>
/// The identity step of the demo seed is all-or-nothing: a failure in the middle leaves no organization, team or account
/// behind, so a re-run creates the whole catalogue and returns every password.
/// </summary>
public sealed class IdentityDemoImportTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly string BlockedEmail = DemoAccountCatalog.EmailOf(DemoAccountCatalog.Accounts[6]);

    private readonly Guid _organizationId = Guid.CreateVersion7();

    private DemoOrganizationProfile Profile => new(
        _organizationId,
        "Parley Demo",
        "parley-demo-" + _organizationId.ToString("N")[..8],
        "Europe/Kyiv",
        CultureCodes.Ukrainian,
        CrmDemoContract.ExportCurrency);

    public async ValueTask InitializeAsync() => await postgres.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_failure_in_the_middle_leaves_nothing_behind_and_a_rerun_creates_every_account()
    {
        var blocker = await CreateBlockingUserAsync();

        var failed = () => ImportAsync();
        await failed.Should().ThrowAsync<InvalidOperationException>();

        await using (var scope = postgres.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(_organizationId);
            (await db.Organizations.AnyAsync(row => row.Id == _organizationId, TestContext.Current.CancellationToken)).Should().BeFalse();
            (await db.Teams.CountAsync(row => row.Name == DemoAccountCatalog.NorthTeamName, TestContext.Current.CancellationToken)).Should().Be(0);
            (await db.Users.CountAsync(row => row.Email!.EndsWith("@" + DemoAccountCatalog.EmailDomain), TestContext.Current.CancellationToken)).Should().Be(0,
                "the accounts created before the failure are rolled back with it");
        }

        await DeleteAsync(blocker);

        var rerun = await ImportAsync();

        rerun.OrganizationCreated.Should().BeTrue();
        rerun.UsersCreated.Should().Be(DemoAccountCatalog.Accounts.Count);
        rerun.Accounts.Should().HaveCount(DemoAccountCatalog.Accounts.Count);
        rerun.Accounts.All(account => account.Password is not null).Should().BeTrue("every created account returns its password");
    }

    [Fact]
    public async Task A_successful_run_commits_every_account_and_membership()
    {
        var result = await ImportAsync();

        result.UsersCreated.Should().Be(DemoAccountCatalog.Accounts.Count);
        result.MembershipsCreated.Should().Be(DemoAccountCatalog.Accounts.Count);
        result.Accounts.All(account => account.Password is not null).Should().BeTrue();

        await using var scope = postgres.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await db.Users.CountAsync(row => row.Email!.EndsWith("@" + DemoAccountCatalog.EmailDomain), TestContext.Current.CancellationToken))
            .Should().Be(DemoAccountCatalog.Accounts.Count);
    }

    private async Task<IdentityDemoSeedResult> ImportAsync()
    {
        await using var scope = postgres.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IIdentityDemoDataImporter>()
            .ImportAsync(Profile, resetPasswords: false, TestContext.Current.CancellationToken);
    }

    /// <summary>A user that owns the username of the seventh demo account but has another address, so creating that account fails.</summary>
    private async Task<User> CreateBlockingUserAsync()
    {
        await using var scope = postgres.Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(_organizationId);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blocker = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = BlockedEmail,
            Email = "blocker@example.test",
            EmailConfirmed = true,
            DisplayName = "Blocker",
            Locale = CultureCodes.English,
            TimeZone = "UTC",
        };
        var result = await userManager.CreateAsync(blocker, DemoPasswordGenerator.Generate(DemoAccountCatalog.PasswordLength));
        result.Succeeded.Should().BeTrue();
        return blocker;
    }

    private async Task DeleteAsync(User user)
    {
        await using var scope = postgres.Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(_organizationId);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        (await userManager.DeleteAsync(user)).Succeeded.Should().BeTrue();
    }
}
