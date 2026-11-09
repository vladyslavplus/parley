namespace Parley.Crm.Shared.Notes;

/// <summary>A note row as the list and the detail read it.</summary>
public sealed record NoteRow(
    Guid Id,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string Body,
    Guid AuthorUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
