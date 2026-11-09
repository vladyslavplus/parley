using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Notes;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Notes.Update;

public sealed class UpdateNoteCommandHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    ICurrentUser currentUser,
    OwnerDirectory ownerDirectory) : IRequestHandler<UpdateNoteCommand, Result<NoteView>>
{
    public async ValueTask<Result<NoteView>> Handle(UpdateNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await NoteLookup.FindChangeableAsync(dbContext, dataScope, currentUser.RequireUserId(), command.Id, cancellationToken);
        if (note is null)
        {
            return Result<NoteView>.Failure(Error.NotFound(CrmErrorCodes.NoteNotFound));
        }

        note.Body = TextValues.Required(command.Body);

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(note);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<NoteView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var row = await NoteLookup.FindRowAsync(dbContext, dataScope, note.Id, cancellationToken);
        return row is null
            ? Result<NoteView>.Failure(Error.NotFound(CrmErrorCodes.NoteNotFound))
            : Result<NoteView>.Success(await NoteViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
