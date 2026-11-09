using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Teams.Update;

public sealed class UpdateTeamCommandHandler(
    IdentityDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UpdateTeamCommand, Result<TeamView>>
{
    public async ValueTask<Result<TeamView>> Handle(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var name = command.Name.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var team = await dbContext.Teams.FirstOrDefaultAsync(row => row.Id == command.TeamId, cancellationToken);
        if (team is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<TeamView>.Failure(Error.NotFound(IdentityErrorCodes.TeamNotFound));
        }

        if (await TeamNames.IsTakenAsync(dbContext, name, team.Id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<TeamView>.Failure(Error.Conflict(IdentityErrorCodes.TeamNameTaken));
        }

        team.Name = name;
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.TeamRenamed, timeProvider, AuditTargetTypes.Team, team.Id);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, IdentityDbContext.TeamNameIndex))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<TeamView>.Failure(Error.Conflict(IdentityErrorCodes.TeamNameTaken));
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<TeamView>.Success(new TeamView(team.Id, team.Name));
    }
}
