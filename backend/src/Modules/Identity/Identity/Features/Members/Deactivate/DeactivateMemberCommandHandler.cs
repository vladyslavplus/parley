using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Members.Deactivate;

public sealed class DeactivateMemberCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeactivateMemberCommand, Result>
{
    public async ValueTask<Result> Handle(DeactivateMemberCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await MemberRules.LockOrganizationAsync(dbContext, organizationId, cancellationToken);

        var membership = await dbContext.Memberships.FirstOrDefaultAsync(row => row.Id == command.MemberId, cancellationToken);
        if (membership is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.MemberNotFound));
        }

        if (membership.Status == MembershipStatuses.Deactivated)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }

        // The last-administrator rule is checked first, so the sole administrator who tries to
        // deactivate themselves gets the last-administrator refusal.
        if (await MemberRules.IsLastActiveAdminAsync(dbContext, membership, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.Conflict(IdentityErrorCodes.MemberLastAdmin));
        }

        if (membership.UserId == actorId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.Conflict(IdentityErrorCodes.MemberSelfDeactivation));
        }

        membership.Status = MembershipStatuses.Deactivated;
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.MemberDeactivated, timeProvider, AuditTargetTypes.Member, membership.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
