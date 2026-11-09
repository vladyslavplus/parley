namespace Parley.Crm.Shared.Activities;

/// <summary>A timeline entry with the names it shows, read in one statement.</summary>
public sealed record ActivityRow(
    Guid Id,
    string Type,
    DateTime OccurredAt,
    Guid? ActorUserId,
    Guid? ContactId,
    string? ContactFirstName,
    string? ContactLastName,
    Guid? DealId,
    string? DealTitle,
    Guid? CompanyId,
    string? CompanyName,
    Guid? NoteId,
    string? NoteBody,
    uint? NoteVersion,
    Guid? TaskId,
    string? TaskTitle,
    string Payload);
