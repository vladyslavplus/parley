using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Teams.Delete;

public sealed class DeleteTeamCommandHandler(
    IdentityDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteTeamCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var team = await dbContext.Teams.FirstOrDefaultAsync(row => row.Id == command.TeamId, cancellationToken);
        if (team is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.TeamNotFound));
        }

        dbContext.Teams.Remove(team);
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.TeamDeleted, timeProvider, AuditTargetTypes.Team, team.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
