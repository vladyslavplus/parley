using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authorization.Permissions;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Domain.Users;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Profile.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    RolePermissionCache rolePermissions) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserView>>
{
    public async ValueTask<Result<CurrentUserView>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var organizationId = tenantContext.OrganizationId;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<CurrentUserView>.Failure(Error.NotFound(IdentityErrorCodes.UserNotFound));
        }

        var membership = await dbContext.Memberships
            .AsNoTracking()
            .Where(row => row.UserId == userId && row.Status == MembershipStatuses.Active)
            .Select(row => new { row.RoleId, row.TeamId })
            .FirstOrDefaultAsync(cancellationToken);
        if (membership is null)
        {
            return Result<CurrentUserView>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthTokenInvalid));
        }

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .Where(row => row.Id == organizationId)
            .Select(row => new CurrentOrganizationView(row.Id, row.Name, row.Slug, row.TimeZone, row.DefaultLocale, row.Currency))
            .FirstOrDefaultAsync(cancellationToken);
        if (organization is null)
        {
            return Result<CurrentUserView>.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound));
        }

        var roleCode = await dbContext.Roles
            .AsNoTracking()
            .Where(row => row.Id == membership.RoleId)
            .Select(row => row.Code)
            .FirstAsync(cancellationToken);
        var grants = await rolePermissions.GetAsync(membership.RoleId, cancellationToken);

        var profile = new UserProfileView(user.Id, user.Email ?? string.Empty, user.DisplayName, user.Locale, user.TimeZone, user.Theme);
        var permissions = grants
            .OrderBy(grant => grant.Permission, StringComparer.Ordinal)
            .Select(grant => new PermissionGrantView(grant.Permission, grant.Scope))
            .ToArray();
        return Result<CurrentUserView>.Success(new CurrentUserView(profile, organization, roleCode, membership.TeamId, permissions));
    }
}
