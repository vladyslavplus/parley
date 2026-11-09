using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Invitations.Revoke;

public sealed class RevokeInvitationCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<RevokeInvitationCommand, Result>
{
    public async ValueTask<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.Invitations.FirstOrDefaultAsync(row => row.Id == command.InvitationId, cancellationToken);
        if (invitation is null)
        {
            return Result.Failure(Error.NotFound(IdentityErrorCodes.InvitationNotFound));
        }

        if (invitation.AcceptedAt is not null || invitation.RevokedAt is not null)
        {
            return Result.Failure(Error.Conflict(IdentityErrorCodes.InvitationNotPending));
        }

        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        // Written without the row-version check: the delivery job of this invitation may rewrite the row
        // after it was loaded, and that must not turn a revoke into a conflict. The predicate keeps the
        // revoke atomic: an invitation accepted or revoked in the meantime matches no row.
        var revoked = await dbContext.Invitations
            .Where(row => row.Id == invitation.Id && row.AcceptedAt == null && row.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.RevokedAt, nowUtc), cancellationToken);
        if (revoked == 0)
        {
            return Result.Failure(Error.Conflict(IdentityErrorCodes.InvitationNotPending));
        }

        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.InvitationRevoked, timeProvider, AuditTargetTypes.Invitation, invitation.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
