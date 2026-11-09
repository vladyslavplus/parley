using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Domain.Invitations;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Options.Invitations;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Invitations.Invite;

public sealed class InviteMemberCommandHandler(
    IdentityDbContext dbContext,
    UserManager<User> userManager,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider,
    IOptions<InvitationOptions> options,
    AuthEmailScheduler emailScheduler) : IRequestHandler<InviteMemberCommand, Result<InvitationView>>
{
    public async ValueTask<Result<InvitationView>> Handle(InviteMemberCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var email = command.Email.Trim().ToLowerInvariant();

        var roleId = await MemberRules.FindSystemRoleIdAsync(dbContext, command.RoleCode, cancellationToken);
        if (roleId is null)
        {
            return Result<InvitationView>.Failure(Error.Validation(IdentityErrorCodes.MemberRoleInvalid));
        }

        if (command.TeamId is { } teamId && !await dbContext.Teams.AnyAsync(team => team.Id == teamId, cancellationToken))
        {
            return Result<InvitationView>.Failure(Error.NotFound(IdentityErrorCodes.TeamNotFound));
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null && await dbContext.Memberships.AnyAsync(
                membership => membership.UserId == existingUser.Id && membership.Status == MembershipStatuses.Active,
                cancellationToken))
        {
            return Result<InvitationView>.Failure(Error.Conflict(IdentityErrorCodes.MemberAlreadyMember));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var open = await dbContext.Invitations
            .Where(invitation => invitation.EmailNormalized == email
                && invitation.AcceptedAt == null
                && invitation.RevokedAt == null)
            .ToListAsync(cancellationToken);
        // A pending invitation is refreshed instead of refused. If its earlier delivery failed,
        // the address could otherwise never be invited again without a manual revoke.
        var pending = open.FirstOrDefault(invitation => invitation.ExpiresAt > nowUtc);
        if (pending is not null)
        {
            var refreshedExpiresAt = nowUtc + options.Value.Lifetime;
            // Written without the row-version check: the delivery job of the earlier invite may be
            // rewriting this row's token at the same moment, and that must not turn into a conflict.
            await dbContext.Invitations
                .Where(row => row.Id == pending.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.RoleId, roleId.Value)
                    .SetProperty(row => row.TeamId, command.TeamId)
                    .SetProperty(row => row.ExpiresAt, refreshedExpiresAt), cancellationToken);
            outbox.AddAuditRecorded(actorId, organizationId, AuditActions.InvitationCreated, timeProvider, AuditTargetTypes.Invitation, pending.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // The delivery job rotates the token, so the newest link is the only one that works.
            emailScheduler.ScheduleInvitationEmail(organizationId, pending.Id, command.Culture);
            return Result<InvitationView>.Success(new InvitationView(pending.Id, email, command.RoleCode, command.TeamId, refreshedExpiresAt));
        }

        // Expired invitations are closed so they stop blocking a new one for the same address.
        foreach (var expired in open)
        {
            expired.RevokedAt = nowUtc;
        }

        var invitation = new Invitation(organizationId)
        {
            EmailNormalized = email,
            RoleId = roleId.Value,
            TeamId = command.TeamId,
            TokenHash = InvitationTokens.CreateUnknownHash(),
            ExpiresAt = nowUtc + options.Value.Lifetime,
            InvitedBy = actorId,
        };
        dbContext.Invitations.Add(invitation);
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.InvitationCreated, timeProvider, AuditTargetTypes.Invitation, invitation.Id);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, IdentityDbContext.InvitationPendingIndex))
        {
            // A parallel invite for the same address won the pending index.
            await transaction.RollbackAsync(cancellationToken);
            return Result<InvitationView>.Failure(Error.Conflict(IdentityErrorCodes.InvitationAlreadyPending));
        }

        await transaction.CommitAsync(cancellationToken);

        // Only after the commit: the delivery job reads the committed invitation.
        emailScheduler.ScheduleInvitationEmail(organizationId, invitation.Id, command.Culture);
        return Result<InvitationView>.Success(new InvitationView(invitation.Id, email, command.RoleCode, command.TeamId, invitation.ExpiresAt));
    }
}
