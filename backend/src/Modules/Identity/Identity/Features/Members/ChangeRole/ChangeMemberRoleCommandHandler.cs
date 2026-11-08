using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Members.ChangeRole;

public sealed class ChangeMemberRoleCommandHandler(
    IdentityDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<ChangeMemberRoleCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var roleId = await MemberRules.FindSystemRoleIdAsync(dbContext, command.RoleCode, cancellationToken);
        if (roleId is null)
        {
            return Result.Failure(Error.Validation(IdentityErrorCodes.MemberRoleInvalid));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await MemberRules.LockOrganizationAsync(dbContext, organizationId, cancellationToken);

        var membership = await dbContext.Memberships.FirstOrDefaultAsync(row => row.Id == command.MemberId, cancellationToken);
        if (membership is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.MemberNotFound));
        }

        var previousRoleId = membership.RoleId;
        if (previousRoleId == roleId.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }

        if (roleId.Value != SystemRoleSeed.AdminRoleId && await MemberRules.IsLastActiveAdminAsync(dbContext, membership, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.Conflict(IdentityErrorCodes.MemberLastAdmin));
        }

        membership.RoleId = roleId.Value;
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.MemberRoleChanged, timeProvider, AuditTargetTypes.Member, membership.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
