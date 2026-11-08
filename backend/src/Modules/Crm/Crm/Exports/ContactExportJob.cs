using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Implementations;

namespace Parley.Crm.Exports;

/// <summary>
/// Writes one contact export file. The job runs as the requester (the ambient job user), so the
/// file holds exactly the rows that requester's data scope allows, under the saved filter.
/// The file is written to a temporary name and moved into place, so a download never sees a
/// partial file. A completed export is skipped, so a retry or a duplicate enqueue writes nothing.
/// </summary>
public sealed class ContactExportJob(
    IMutableTenantContext tenantContext,
    JobCurrentUser jobUser,
    CrmDbContext dbContext,
    IDataScope dataScope,
    ContactExportStorage storage,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    private const string TemporaryExtension = ".tmp";

    private const int FileBufferSize = 64 * 1024;

    private Guid _exportId;

    /// <summary>Entry point enqueued by the scheduler.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid exportId, CancellationToken cancellationToken)
    {
        _exportId = exportId;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var export = await dbContext.ContactExports.FirstOrDefaultAsync(row => row.Id == _exportId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (export is null
            || export.Status == ContactExportStatuses.Completed
            || export.ExpiresAt <= now)
        {
            return;
        }

        export.Status = ContactExportStatuses.Running;
        await dbContext.SaveChangesAsync(cancellationToken);

        jobUser.Set(export.RequestedByUserId);
        try
        {
            var fileName = ContactExportFiles.FileNameFor(export.Id);
            var path = storage.PathFor(fileName);
            Directory.CreateDirectory(storage.Root);
            var temporary = path + TemporaryExtension;

            var rowCount = await WriteFileAsync(export, temporary, cancellationToken);
            File.Move(temporary, path, overwrite: true);

            export.Status = ContactExportStatuses.Completed;
            export.FileName = fileName;
            export.RowCount = rowCount;
            export.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            export.Status = ContactExportStatuses.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            jobUser.Clear();
        }
    }

    private async Task<int> WriteFileAsync(ContactExport export, string path, CancellationToken cancellationToken)
    {
        var filter = JsonSerializer.Deserialize<ContactListFilter>(export.FilterJson)
            ?? throw new InvalidOperationException("The export filter snapshot is empty.");
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsExport, cancellationToken);
        var rows = ContactListFilters.Apply(ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope), filter)
            .OrderBy(contact => contact.NameNormalized)
            .ThenBy(contact => contact.Id)
            .Select(contact => new ContactExportRow(
                contact.Id,
                contact.FirstName,
                contact.LastName,
                contact.Email,
                contact.Phone,
                contact.JobTitle,
                contact.Country,
                contact.City,
                contact.CompanyId,
                contact.OwnerUserId,
                contact.CreatedAt));

        var rowCount = 0;
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, FileBufferSize, useAsync: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync(ContactExportCsv.Header.AsMemory(), cancellationToken);
        await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(ContactExportCsv.FormatRow(row).AsMemory(), cancellationToken);
            rowCount++;
        }

        await writer.FlushAsync(cancellationToken);
        return rowCount;
    }
}
