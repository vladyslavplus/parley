using Parley.Crm.Domain.Exports;

namespace Parley.Crm.Features.Contacts.Exports.Shared;

public static class ContactExportViews
{
    public static ContactExportView ToView(ContactExport export, DateTime nowUtc) => new(
        export.Id,
        export.Status,
        export.RowCount,
        export.CreatedAt,
        export.CompletedAt,
        export.ExpiresAt,
        IsDownloadable(export, nowUtc));

    private static bool IsDownloadable(ContactExport export, DateTime nowUtc) =>
        export is { Status: ContactExportStatuses.Completed, FileName: not null } && export.ExpiresAt > nowUtc;
}
