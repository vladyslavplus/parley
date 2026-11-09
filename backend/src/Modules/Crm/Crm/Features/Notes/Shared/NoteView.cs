namespace Parley.Crm.Features.Notes.Shared;

/// <summary>
/// A note as the API returns it. <see cref="Version"/> is the row version: clients send it back on update, and a stale
/// value returns 409.
/// </summary>
/// <param name="Id">Note identifier.</param>
/// <param name="ContactId">Contact the note belongs to, when it belongs to a contact.</param>
/// <param name="DealId">Deal the note belongs to, when it belongs to a deal.</param>
/// <param name="CompanyId">Company the note belongs to, when it belongs to a company.</param>
/// <param name="Body">Note text.</param>
/// <param name="AuthorUserId">Member who wrote the note.</param>
/// <param name="AuthorDisplayName">Author display name, or null when unnamed or outside the organization.</param>
/// <param name="AuthorEmail">Author email, or null under the same conditions.</param>
/// <param name="CreatedAt">Creation instant in UTC.</param>
/// <param name="UpdatedAt">Last change instant in UTC.</param>
/// <param name="Version">Row version to send back on update.</param>
public sealed record NoteView(
    Guid Id,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string Body,
    Guid AuthorUserId,
    string? AuthorDisplayName,
    string? AuthorEmail,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
