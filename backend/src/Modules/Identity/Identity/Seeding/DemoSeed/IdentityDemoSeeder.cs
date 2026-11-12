using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.DemoSeed;
using Parley.Identity.Domain.DemoSeed;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Domain.Organizations;
using Parley.Identity.Domain.Teams;
using Parley.Identity.Domain.Users;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Seeding.DemoSeed;

/// <summary>
/// Creates the demo organization, its teams, its accounts and their memberships. Every demo account gets the configured
/// development password, existing accounts included, so the documented sign-in always works. The marker of the last complete
/// seed lives in <c>identity.demo_seed_states</c>.
/// </summary>
public sealed class IdentityDemoSeeder(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    IMutableTenantContext tenantContext,
    TimeProvider timeProvider) : IIdentityDemoSeeder
{
    public async Task<IdentityDemoSeedResult> EnsureAsync(
        DemoOrganizationProfile organization,
        string accountPassword,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountPassword);
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            async ct => await EnsureInTransactionAsync(organization, accountPassword, ct),
            cancellationToken);
    }

    public async Task<DateTime?> ReadSeededAtAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.DemoSeedStates
            .AsNoTracking()
            .Where(state => state.OrganizationId == organizationId)
            .Select(state => (DateTime?)state.SeededAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task MarkSeededAsync(Guid organizationId, DateTime seededAtUtc, CancellationToken cancellationToken)
    {
        var state = await dbContext.DemoSeedStates.FirstOrDefaultAsync(row => row.OrganizationId == organizationId, cancellationToken);
        if (state is null)
        {
            dbContext.DemoSeedStates.Add(new DemoSeedState { OrganizationId = organizationId, SeededAt = seededAtUtc });
        }
        else
        {
            state.SeededAt = seededAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// One transaction for the whole identity step: either every account of the catalogue exists with its password, or
    /// nothing changed.
    /// </summary>
    private async Task<IdentityDemoSeedResult> EnsureInTransactionAsync(
        DemoOrganizationProfile organization,
        string accountPassword,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var organizationCreated = await EnsureOrganizationAsync(organization, now, cancellationToken);

        tenantContext.Set(organization.Id);
        try
        {
            var (teamsCreated, teamIds) = await EnsureTeamsAsync(organization.Id, cancellationToken);
            var accounts = new List<DemoAccount>(DemoAccountCatalog.Accounts.Count);
            var usersCreated = 0;
            var membershipsCreated = 0;

            foreach (var entry in DemoAccountCatalog.Accounts)
            {
                var email = DemoAccountCatalog.EmailOf(entry);
                var user = await userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    user = await CreateUserAsync(organization, email, entry.DisplayName, accountPassword);
                    usersCreated++;
                }
                else
                {
                    await SetPasswordAsync(user, accountPassword);
                }

                var teamId = entry.TeamName is null ? (Guid?)null : teamIds[entry.TeamName];
                if (await AddMembershipIfMissingAsync(organization.Id, user.Id, entry.RoleId, teamId, now, cancellationToken))
                {
                    membershipsCreated++;
                }

                accounts.Add(new DemoAccount(user.Id, email, entry.DisplayName, entry.RoleCode, entry.TeamName, accountPassword));
            }

            await transaction.CommitAsync(cancellationToken);
            return new IdentityDemoSeedResult(organizationCreated, teamsCreated, usersCreated, membershipsCreated, accounts);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    private async Task<bool> EnsureOrganizationAsync(DemoOrganizationProfile organization, DateTime now, CancellationToken cancellationToken)
    {
        if (await dbContext.Organizations.AnyAsync(row => row.Id == organization.Id, cancellationToken))
        {
            return false;
        }

        dbContext.Organizations.Add(new Organization
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            TimeZone = organization.TimeZone,
            DefaultLocale = organization.Locale,
            Currency = organization.Currency,
            Status = OrganizationStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<(int Created, Dictionary<string, Guid> Ids)> EnsureTeamsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var ids = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var created = 0;
        foreach (var name in DemoAccountCatalog.TeamNames)
        {
            var team = await dbContext.Teams.FirstOrDefaultAsync(row => row.Name == name, cancellationToken);
            if (team is null)
            {
                team = new Team(organizationId, name);
                dbContext.Teams.Add(team);
                created++;
            }

            ids[name] = team.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (created, ids);
    }

    private async Task<User> CreateUserAsync(DemoOrganizationProfile organization, string email, string displayName, string password)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Locale = organization.Locale,
            TimeZone = organization.TimeZone,
            LastActiveOrganizationId = organization.Id,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // Only platform validation codes go into the message; the password is never part of it.
            throw new InvalidOperationException(
                "A demo account could not be created: " + string.Join(", ", result.Errors.Select(error => error.Code)));
        }

        return user;
    }

    private async Task SetPasswordAsync(User user, string password)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "A demo password could not be set: " + string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }

    private async Task<bool> AddMembershipIfMissingAsync(
        Guid organizationId,
        Guid userId,
        Guid roleId,
        Guid? teamId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Memberships.AnyAsync(row => row.UserId == userId, cancellationToken))
        {
            return false;
        }

        dbContext.Memberships.Add(new Membership(organizationId, userId, roleId, teamId, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
