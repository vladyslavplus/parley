using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Organizations.Update;

public sealed class UpdateOrganizationCommandHandler(
    IdentityDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<IdentityDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UpdateOrganizationCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        if (command.OrganizationId != organizationId)
        {
            return Result.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound));
        }

        var actorId = currentUser.RequireUserId();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(row => row.Id == organizationId, cancellationToken);
        if (organization is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.NotFound(IdentityErrorCodes.OrganizationNotFound));
        }

        organization.Name = command.Name.Trim();
        organization.TimeZone = command.TimeZone;
        organization.DefaultLocale = command.DefaultLocale;
        organization.Currency = command.Currency;
        organization.UpdatedAt = nowUtc;
        outbox.AddAuditRecorded(actorId, organizationId, AuditActions.OrganizationSettingsChanged, timeProvider, AuditTargetTypes.Organization, organizationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
