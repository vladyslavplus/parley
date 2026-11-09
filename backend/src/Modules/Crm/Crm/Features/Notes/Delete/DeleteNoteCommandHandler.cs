using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Notes.Delete;

public sealed class DeleteNoteCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteNoteCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await NoteLookup.FindChangeableAsync(dbContext, dataScope, currentUser.RequireUserId(), command.Id, cancellationToken);
        if (note is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.NoteNotFound));
        }

        note.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.NoteDeleted, timeProvider, CrmAuditTargetTypes.Note, note.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
