using Mediator;
using Parley.Crm.Features.Notes.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Notes.Update;

/// <summary>
/// Replaces the text of a note. The subject stays fixed. <paramref name="Version"/> is the row version the client read;
/// a stale value returns 409.
/// </summary>
/// <param name="Id">Note identifier.</param>
/// <param name="Version">Row version the client read.</param>
/// <param name="Body">Note text.</param>
[RequiresPermission(Permissions.ActivitiesWrite)]
public sealed record UpdateNoteCommand(Guid Id, uint Version, string Body) : IRequest<Result<NoteView>>;
