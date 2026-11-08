using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Teams;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Teams.Create;

public sealed class CreateTeamCommandHandler(
    IdentityDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IOutbox outbox,
    TimeProvider timeProvider) : IRequestHandler<CreateTeamCommand, Result<TeamView>>
{
    public async ValueTask<Result<TeamView>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var name = command.Name.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await TeamNames.IsTakenAsync(dbContext, name, excludedTeamId: null, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<TeamView>.Failure(Error.Conflict(IdentityErrorCodes.TeamNameTaken));
        }

        var team = new Team(organizationId, name);
        dbContext.Teams.Add(team);
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.TeamCreated, timeProvider, AuditTargetTypes.Team, team.Id);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, IdentityDbContext.TeamNameIndex))
        {
            // A parallel create of the same name won the unique index.
            await transaction.RollbackAsync(cancellationToken);
            return Result<TeamView>.Failure(Error.Conflict(IdentityErrorCodes.TeamNameTaken));
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<TeamView>.Success(new TeamView(team.Id, team.Name));
    }
}
