using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Notes;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Subjects;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Notes.Create;

public sealed class CreateNoteCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    TimeProvider timeProvider) : IRequestHandler<CreateNoteCommand, Result<NoteView>>
{
    public async ValueTask<Result<NoteView>> Handle(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();
        var unreadable = await SubjectChecks.FirstUnreadableAsync(dbContext, dataScope, command.ContactId, command.DealId, command.CompanyId, cancellationToken);
        if (unreadable is not null)
        {
            return Result<NoteView>.Failure(CrmErrors.Field(unreadable, CrmErrorCodes.NoteSubjectNotFound));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var note = new Note
        {
            OrganizationId = tenantContext.OrganizationId,
            Body = TextValues.Required(command.Body),
            ContactId = command.ContactId,
            DealId = command.DealId,
            CompanyId = command.CompanyId,
            AuthorUserId = actorId,
        };
        dbContext.Notes.Add(note);
        dbContext.Activities.Add(new Activity
        {
            OrganizationId = tenantContext.OrganizationId,
            Type = ActivityTypes.NoteAdded,
            ContactId = command.ContactId,
            DealId = command.DealId,
            CompanyId = command.CompanyId,
            NoteId = note.Id,
            ActorUserId = actorId,
            OccurredAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await NoteLookup.FindRowAsync(dbContext, dataScope, note.Id, cancellationToken);
        return row is null
            ? Result<NoteView>.Failure(Error.NotFound(CrmErrorCodes.NoteNotFound))
            : Result<NoteView>.Success(await NoteViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
