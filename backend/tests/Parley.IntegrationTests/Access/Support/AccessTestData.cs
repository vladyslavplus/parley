using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Domain.Roles;
using Parley.Identity.Domain.Users;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Access.Support;

/// <summary>
/// Writes access-model rows directly through the identity DbContext, the way the
/// later endpoints will, so tests can set up members, roles and grants without HTTP.
/// Every method opens its own scope and never keeps a context.
/// </summary>
public static class AccessTestData
{
    /// <summary>
    /// Creates a confirmed user whose last active organization is <paramref name="organizationId"/>
    /// and gives it a membership with the role. Returns the user id.
    /// </summary>
    public static async Task<Guid> AddMemberAsync(
        IServiceProvider services,
        Guid organizationId,
        Guid roleId,
        string email,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Locale = Parley.SharedKernel.Localization.CultureCodes.English,
            LastActiveOrganizationId = organizationId,
        };
        var created = await userManager.CreateAsync(user, AuthTestClient.Password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException("The test user could not be created.");
        }

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        tenant.Set(organizationId);
        try
        {
            db.Memberships.Add(new Membership(
                organizationId,
                user.Id,
                roleId,
                teamId: null,
                joinedAt: timeProvider.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }

        return user.Id;
    }

    public static async Task SetMembershipStatusAsync(
        IServiceProvider services,
        Guid organizationId,
        Guid userId,
        string status,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        tenant.Set(organizationId);
        try
        {
            await db.Memberships
                .Where(membership => membership.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(membership => membership.Status, status), cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    /// <summary>Creates an organization-owned role with one organization-scoped grant of <paramref name="permission"/>.</summary>
    public static async Task<Guid> AddCustomRoleAsync(
        IServiceProvider services,
        Guid organizationId,
        string code,
        string permission,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Code = code,
            IsSystem = false,
        };
        db.Roles.Add(role);
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            Permission = permission,
            Scope = RoleScopeNames.Organization,
        });
        await db.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    public static async Task RemoveGrantAsync(
        IServiceProvider services,
        Guid roleId,
        string permission,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.RolePermissions
            .Where(grant => grant.RoleId == roleId && grant.Permission == permission)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Reads the organization of a user's membership, ignoring the tenant filter (test lookup only).</summary>
    public static async Task<Guid> ReadOrganizationIdAsync(IServiceProvider services, Guid userId, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Memberships
            .IgnoreQueryFilters()
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.OrganizationId)
            .SingleAsync(cancellationToken);
    }
}
