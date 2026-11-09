using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Exports;

/// <summary>
/// A requested contact CSV export. The row is created in the request, the file is written by a
/// background job, and the row is deleted together with its file after <see cref="ExpiresAt"/>.
/// <see cref="FilterJson"/> is the list filter snapshot the job replays under the requester's scope.
/// </summary>
public sealed class ContactExport : BusinessEntity
{
    public ContactExport()
    {
    }

    public ContactExport(Guid organizationId, Guid requestedByUserId, string filterJson, DateTime expiresAt)
        : base(organizationId)
    {
        RequestedByUserId = requestedByUserId;
        FilterJson = filterJson;
        ExpiresAt = expiresAt;
    }

    public Guid RequestedByUserId { get; set; }

    public string Status { get; set; } = ContactExportStatuses.Queued;

    public string FilterJson { get; set; } = CrmLimits.EmptyJsonObject;

    /// <summary>Server-generated file name inside the export directory; never user input.</summary>
    public string? FileName { get; set; }

    public int? RowCount { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
