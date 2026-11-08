using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Domain.Invitations;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Domain.Users;
using Parley.Identity.Features.Accounts.Shared;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Invitations.Accept;

public sealed class AcceptInvitationCommandHandler(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    RefreshTokenService refreshTokens,
    IMutableTenantContext tenantContext,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<AcceptInvitationCommand, Result<AuthTokens>>
{
    public async ValueTask<Result<AuthTokens>> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var hash = InvitationTokens.Hash(command.Token);

        // No tenant is active on an anonymous request. The row names its organization, and
        // the lookup is by the token hash only.
        var invitation = await dbContext.Invitations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.TokenHash == hash, cancellationToken);
        if (invitation is null
            || !InvitationTokens.Matches(invitation.TokenHash, hash)
            || invitation.AcceptedAt is not null
            || invitation.RevokedAt is not null
            || invitation.ExpiresAt <= nowUtc)
        {
            return Result<AuthTokens>.Failure(Error.Validation(IdentityErrorCodes.InvitationTokenInvalid));
        }

        tenantContext.Set(invitation.OrganizationId);
        try
        {
            return await AcceptAsync(command, invitation, nowUtc, cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    private async Task<Result<AuthTokens>> AcceptAsync(
        AcceptInvitationCommand command,
        Invitation invitation,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var organizationId = invitation.OrganizationId;
        var existing = await userManager.FindByEmailAsync(invitation.EmailNormalized);
        // An unconfirmed account was never proven by its owner, so its password is not checked:
        // the invitation token proves the mailbox and the submitted password replaces it below.
        var proveUnconfirmed = existing is { EmailConfirmed: false };
        if (existing is not null && !proveUnconfirmed)
        {
            if (await userManager.IsLockedOutAsync(existing))
            {
                return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthInvalidCredentials));
            }

            if (!await userManager.CheckPasswordAsync(existing, command.Password))
            {
                await userManager.AccessFailedAsync(existing);
                return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthInvalidCredentials));
            }

            await userManager.ResetAccessFailedCountAsync(existing);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = existing;
        if (user is null)
        {
            // The invitation was sent to this address, so the address is confirmed by the link.
            user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = invitation.EmailNormalized,
                Email = invitation.EmailNormalized,
                EmailConfirmed = true,
                Locale = command.Culture,
                LastActiveOrganizationId = organizationId,
            };
            IdentityResult created;
            try
            {
                created = await userManager.CreateAsync(user, command.Password);
            }
            catch (DbUpdateException exception) when (UserCreationErrors.IsDuplicateAccount(exception))
            {
                // A parallel registration of the same address won the unique index.
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthTokens>.Failure(Error.Conflict(IdentityErrorCodes.UserEmailTaken));
            }

            if (!created.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthTokens>.Failure(UserCreationErrors.Map(created));
            }
        }
        else if (proveUnconfirmed)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, resetToken, command.Password);
            if (!reset.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthTokens>.Failure(UserCreationErrors.Map(reset));
            }

            // Sessions opened under the squatter's password end; the address is now confirmed.
            await userManager.UpdateSecurityStampAsync(user);
            await refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        // Only one request can move the invitation from pending to accepted.
        var claimed = await dbContext.Invitations
            .Where(row => row.Id == invitation.Id && row.AcceptedAt == null && row.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.AcceptedAt, nowUtc), cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AuthTokens>.Failure(Error.Validation(IdentityErrorCodes.InvitationTokenInvalid));
        }

        var membership = await dbContext.Memberships.FirstOrDefaultAsync(row => row.UserId == user.Id, cancellationToken);
        if (membership is null)
        {
            dbContext.Memberships.Add(new Membership(organizationId, user.Id, invitation.RoleId, invitation.TeamId, nowUtc));
        }
        else if (membership.Status == MembershipStatuses.Active)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AuthTokens>.Failure(Error.Conflict(IdentityErrorCodes.MemberAlreadyMember));
        }
        else
        {
            // A deactivated member who is invited again comes back with the invited role and team.
            membership.Status = MembershipStatuses.Active;
            membership.RoleId = invitation.RoleId;
            membership.TeamId = invitation.TeamId;
            outbox.AddAuditRecorded(user.Id, organizationId, AuditActions.MemberReactivated, timeProvider, AuditTargetTypes.Member, membership.Id);
        }

        outbox.AddAuditRecorded(user.Id, organizationId, AuditActions.InvitationAccepted, timeProvider, AuditTargetTypes.Invitation, invitation.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var tokens = await refreshTokens.IssueNewFamilyAsync(user.Id, organizationId, user.IsPlatformOperator, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<AuthTokens>.Success(tokens);
    }
}
