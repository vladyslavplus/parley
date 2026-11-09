namespace Parley.Crm.Features.Contacts.Exports.Shared;

/// <summary>
/// An export as the requester sees it. <see cref="IsDownloadable"/> is true once the file is
/// complete and the link has not expired; the API builds the download address from the id.
/// </summary>
public sealed record ContactExportView(
    Guid Id,
    string Status,
    int? RowCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime ExpiresAt,
    bool IsDownloadable);
