using Mediator;
using Parley.Crm.Features.Notes.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Notes.Create;

/// <summary>
/// Writes a note on exactly one contact, deal or company and adds a <c>note_added</c> entry to that subject's timeline.
/// </summary>
/// <param name="Body">Note text.</param>
/// <param name="ContactId">Contact the note belongs to.</param>
/// <param name="DealId">Deal the note belongs to.</param>
/// <param name="CompanyId">Company the note belongs to.</param>
[RequiresPermission(Permissions.ActivitiesWrite)]
public sealed record CreateNoteCommand(
    string Body,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId) : IRequest<Result<NoteView>>;
