using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Domain.Organizations;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Organizations.Switch;

public sealed class SwitchOrganizationCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    RefreshTokenService refreshTokens) : IRequestHandler<SwitchOrganizationCommand, Result<AuthTokens>>
{
    public async ValueTask<Result<AuthTokens>> Handle(SwitchOrganizationCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        // The target organization is not the active tenant yet, so the membership read names
        // the user and the organization explicitly and skips the tenant filter.
        var isMember = await dbContext.Memberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(membership => membership.UserId == userId
                && membership.OrganizationId == command.OrganizationId
                && membership.Status == MembershipStatuses.Active, cancellationToken);
        var isActive = await dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == command.OrganizationId
                && organization.Status == OrganizationStatuses.Active, cancellationToken);
        if (!isMember || !isActive)
        {
            return Result<AuthTokens>.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound));
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(row => row.Id == userId)
            .Select(row => new { row.IsPlatformOperator })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthTokenInvalid));
        }

        if (!await refreshTokens.EndOwnedSessionAsync(command.RefreshToken, userId, cancellationToken))
        {
            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
        }

        var tokens = await refreshTokens.IssueNewFamilyAsync(userId, command.OrganizationId, user.IsPlatformOperator, cancellationToken);
        await dbContext.Users
            .Where(row => row.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.LastActiveOrganizationId, (Guid?)command.OrganizationId), cancellationToken);
        return Result<AuthTokens>.Success(tokens);
    }
}
