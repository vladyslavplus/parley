using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Members.ChangeTeam;

public sealed class ChangeMemberTeamCommandHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<ChangeMemberTeamCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeMemberTeamCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var membership = await dbContext.Memberships.FirstOrDefaultAsync(row => row.Id == command.MemberId, cancellationToken);
        if (membership is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.MemberNotFound));
        }

        if (command.TeamId is { } teamId && !await dbContext.Teams.AnyAsync(team => team.Id == teamId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.TeamNotFound));
        }

        if (membership.TeamId == command.TeamId)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }

        membership.TeamId = command.TeamId;
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.MemberTeamChanged, timeProvider, AuditTargetTypes.Member, membership.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
