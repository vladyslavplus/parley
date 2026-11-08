using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Identity.Members.Support;

/// <summary>
/// Reads and writes membership and invitation rows directly, for setups that the API
/// does not offer (joining an existing account to a second organization) and for checks
/// that must look at stored values. Each method opens its own scope.
/// </summary>
public static class MembershipTestData
{
    /// <summary>Adds a membership for an existing account in another organization.</summary>
    public static async Task AddMembershipAsync(
        IServiceProvider services,
        Guid organizationId,
        Guid userId,
        Guid roleId,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        tenant.Set(organizationId);
        try
        {
            db.Memberships.Add(new Membership(
                organizationId,
                userId,
                roleId,
                teamId: null,
                joinedAt: timeProvider.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    /// <summary>Reads the membership identifier of a user in an organization.</summary>
    public static async Task<Guid> ReadMembershipIdAsync(IServiceProvider services, Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Memberships
            .IgnoreQueryFilters()
            .Where(membership => membership.OrganizationId == organizationId && membership.UserId == userId)
            .Select(membership => membership.Id)
            .SingleAsync(cancellationToken);
    }

    /// <summary>Reads the stored token hashes of every invitation of an organization.</summary>
    public static async Task<IReadOnlyList<byte[]>> ReadInvitationHashesAsync(IServiceProvider services, Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Invitations
            .IgnoreQueryFilters()
            .Where(invitation => invitation.OrganizationId == organizationId)
            .Select(invitation => invitation.TokenHash)
            .ToListAsync(cancellationToken);
    }
}
