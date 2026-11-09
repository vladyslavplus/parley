using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Notes.Delete;

/// <summary>Soft deletes a note. Its timeline entry stays but no longer shows the text.</summary>
/// <param name="Id">Note identifier.</param>
[RequiresPermission(Permissions.ActivitiesWrite)]
public sealed record DeleteNoteCommand(Guid Id) : IRequest<Result>;
