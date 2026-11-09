using System.Text.Json;

namespace Parley.Crm.Features.Activities.Shared;

/// <summary>
/// One timeline entry as the API returns it. Times are UTC. The names are the ones the caller may see; a link to a
/// record outside the caller's scope shows no name.
/// </summary>
/// <param name="Id">Entry identifier.</param>
/// <param name="Type">Entry type: <c>contact_created</c>, <c>deal_stage_changed</c>, <c>deal_closed</c>, <c>note_added</c> or <c>task_completed</c>.</param>
/// <param name="OccurredAt">Instant the entry happened, UTC.</param>
/// <param name="ActorUserId">Member who caused the entry, when known.</param>
/// <param name="ActorDisplayName">Actor display name, or null when unnamed or outside the organization.</param>
/// <param name="ContactId">Contact of the entry, when set.</param>
/// <param name="ContactName">Full name of that contact, or null.</param>
/// <param name="DealId">Deal of the entry, when set.</param>
/// <param name="DealTitle">Title of that deal, or null.</param>
/// <param name="CompanyId">Company of the entry, when set.</param>
/// <param name="CompanyName">Name of that company, or null.</param>
/// <param name="NoteId">Note the entry refers to, when it is a note entry and the note is live.</param>
/// <param name="NoteBody">Text of that note, or null.</param>
/// <param name="NoteVersion">Row version of that note, for editing it; null for entries that are not note entries.</param>
/// <param name="TaskId">Task the entry refers to, when it is a task entry and the task is live.</param>
/// <param name="TaskTitle">Title of that task, or null.</param>
/// <param name="Payload">Safe details of the entry as a JSON object.</param>
public sealed record ActivityView(
    Guid Id,
    string Type,
    DateTime OccurredAt,
    Guid? ActorUserId,
    string? ActorDisplayName,
    Guid? ContactId,
    string? ContactName,
    Guid? DealId,
    string? DealTitle,
    Guid? CompanyId,
    string? CompanyName,
    Guid? NoteId,
    string? NoteBody,
    uint? NoteVersion,
    Guid? TaskId,
    string? TaskTitle,
    JsonElement Payload);
